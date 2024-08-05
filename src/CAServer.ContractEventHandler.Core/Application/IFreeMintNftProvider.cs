using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.FreeMint.Etos;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Guardian.Provider;
using CAServer.Options;
using CAServer.Signature.Provider;
using Google.Protobuf;
using GraphQL;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Portkey.FreeMint;
using Volo.Abp.DependencyInjection;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IFreeMintNftProvider
{
    Task<TransactionInfoDto> SendMintNftTransactionAsync(FreeMintEto eventData);
}

public class FreeMintNftProvider : IFreeMintNftProvider, ISingletonDependency
{
    private readonly ISignatureProvider _signatureProvider;
    private readonly ChainOptions _chainOptions;
    private readonly ContractServiceOptions _contractServiceOptions;
    private readonly PayRedPackageAccount _packageAccount;
    private readonly ILogger<FreeMintNftProvider> _logger;
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly INESTRepository<CAHolderIndex, Guid> _holderRepository;
    private readonly FreeMintOptions _freeMintOptions;

    public FreeMintNftProvider(ISignatureProvider signatureProvider,
        IOptionsSnapshot<ChainOptions> chainOptions,
        IOptionsSnapshot<ContractServiceOptions> contractGrainOptions,
        IOptionsSnapshot<FreeMintOptions> freeMintOptions,
        IOptionsSnapshot<PayRedPackageAccount> packageAccount, ILogger<FreeMintNftProvider> logger,
        IGraphQLHelper graphQlHelper, INESTRepository<CAHolderIndex, Guid> holderRepository)
    {
        _signatureProvider = signatureProvider;
        _logger = logger;
        _graphQlHelper = graphQlHelper;
        _holderRepository = holderRepository;
        _chainOptions = chainOptions.Value;
        _contractServiceOptions = contractGrainOptions.Value;
        _packageAccount = packageAccount.Value;
        _freeMintOptions = freeMintOptions.Value;
    }

    public async Task<TransactionInfoDto> SendMintNftTransactionAsync(FreeMintEto eventData)
    {
        try
        {
            var from = _packageAccount.getOneAccountRandom();
            _logger.LogInformation("[FreeMint] get free mint account, from is {from} ", from);
            var holder = await GetCaHolderAsync(eventData.UserId);
            if (holder == null)
            {
                _logger.LogWarning("[FreeMint] holder is null, userId:{userId}", eventData.UserId);
                return null;
            }

            var guardiansDto = await GetCaHolderInfoAsync(holder.CaHash);
            var toAddress = guardiansDto.CaHolderInfo.FirstOrDefault()?.CaAddress;
            if (toAddress.IsNullOrEmpty())
            {
                _logger.LogWarning("[FreeMint] guardian info is empty, userId:{userId}, caHash:{caHash}",
                    eventData.UserId,
                    holder.CaHash);
                return null;
            }

            var chainId = _chainOptions.ChainInfos.Keys.First(t => t != CommonConstant.MainChainId);
            var mintNftInput = new MintNftInput()
            {
                Symbol = $"{eventData.CollectionInfo.CollectionName.ToUpper()}-{eventData.ConfirmInfo.TokenId}",
                TokenName = eventData.ConfirmInfo.Name,
                TotalSupply = CommonConstant.FreeMintTotalSupply,
                Decimals = CommonConstant.FreeMintDecimals,
                IsBurnable = true,
                IssueChainId = ChainHelper.ConvertBase58ToChainId(chainId),
                ExternalInfo = new ExternalInfo()
                {
                    Value =
                    {
                        {
                            "__nft_image_url", eventData.ConfirmInfo.ImageUrl
                        }
                    }
                },
                To = Address.FromBase58(toAddress)
            };

            _logger.LogInformation("FreeMint SendMintNftTransactionAsync param: {param}",
                JsonConvert.SerializeObject(mintNftInput));
            return await SendFreeMintAsync(chainId, mintNftInput, from,
                _freeMintOptions.ContractAddress, "MintNft");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "FreeMint SendMintNftTransactionAsync error: {param}",
                JsonConvert.SerializeObject(eventData));
            return null;
        }
    }

    public async Task<TransactionInfoDto> SendFreeMintAsync(string chainId, IMessage param,
        string from, string freeMintAddress, string methodName)
    {
        try
        {
            if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainInfo))
            {
                return null;
            }

            var client = new AElfClient(chainInfo.BaseUrl);
            await client.IsConnectedAsync();
            var ownAddress = client.GetAddressFromPubKey(from); //select public key
            _logger.LogInformation(
                "[FreeMint] Get Address From PubKey, ownAddress：{ownAddress}, ContractAddress: {ContractAddress} ,methodName:{methodName}",
                ownAddress, freeMintAddress, methodName);

            var transaction =
                await client.GenerateTransactionAsync(ownAddress, freeMintAddress, methodName,
                    param);

            var txWithSign = await _signatureProvider.SignTxMsg(from, transaction.GetHash().ToHex());
            transaction.Signature = ByteStringHelper.FromHexString(txWithSign);

            var result = await client.SendTransactionAsync(new SendTransactionInput
            {
                RawTransaction = transaction.ToByteArray().ToHex()
            });
            _logger.LogInformation("[FreeMint] Send transaction, transactionId: {transactionId}", result.TransactionId);

            await Task.Delay(_contractServiceOptions.Delay);

            var transactionResult = await client.GetTransactionResultAsync(result.TransactionId);
            _logger.LogInformation(
                "[FreeMint] query transactionResult, transactionId:{transactionId}, transaction status:{status}",
                transactionResult.TransactionId, transactionResult.Status);

            var times = 0;
            while ((transactionResult.Status == TransactionState.Pending ||
                    transactionResult.Status == TransactionState.NotExisted) &&
                   times < _contractServiceOptions.RetryTimes)
            {
                times++;
                await Task.Delay(_contractServiceOptions.RetryDelay);
                transactionResult = await client.GetTransactionResultAsync(result.TransactionId);
                _logger.LogInformation(
                    "[FreeMint] query transactionResult, transactionId:{transactionId}, transaction status:{status}, times:{times}",
                    transactionResult.TransactionId, transactionResult.Status, times);
            }

            return new TransactionInfoDto
            {
                Transaction = transaction,
                TransactionResultDto = transactionResult
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "FreeMint transaction error: {param}", JsonConvert.SerializeObject(param));
            return null;
        }
    }

    public async Task<CAHolderIndex> GetCaHolderAsync(Guid userId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CAHolderIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.UserId).Value(userId))
        };

        //mustQuery.Add(q => q.Terms(i => i.Field(f => f.IsDeleted).Terms(false)));
        QueryContainer Filter(QueryContainerDescriptor<CAHolderIndex> f) => f.Bool(b => b.Must(mustQuery));

        var holder = await _holderRepository.GetAsync(Filter);
        return holder;
    }

    public async Task<GuardiansDto> GetCaHolderInfoAsync(string caHash, int skipCount = 0,
        int maxResultCount = 10)
    {
        return await _graphQlHelper.QueryAsync<GuardiansDto>(new GraphQLRequest
        {
            Query = @"
			    query($caHash:String,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderInfo(dto: {caHash:$caHash,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            id,chainId,caHash,caAddress,originChainId,managerInfos{address,extraData},guardianList{guardians{verifierId,identifierHash,salt,isLoginGuardian,type}}}
                }",
            Variables = new
            {
                caHash, skipCount, maxResultCount
            }
        });
    }
}