using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using AutoMapper.Internal.Mappers;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Dtos;
using CAServer.Entities.Es;
using CAServer.EnumType;
using CAServer.FreeMint.Dtos;
using CAServer.FreeMint.Etos;
using CAServer.Grains.Grain.FreeMint;
using CAServer.Guardian.Provider;
using Google.Protobuf;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;
using IObjectMapper = Volo.Abp.ObjectMapping.IObjectMapper;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IMintNftItemService
{
    Task MintAsync(FreeMintEto eventData);
}

public class MintNftItemService : IMintNftItemService, ISingletonDependency
{
    private readonly INESTRepository<FreeMintIndex, string> _freeMintRepository;
    private readonly ILogger<MintNftItemService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly INESTRepository<CAHolderIndex, Guid> _holderRepository;
    private readonly IGraphQLHelper _graphQlHelper;

    public MintNftItemService(INESTRepository<FreeMintIndex, string> freeMintRepository,
        ILogger<MintNftItemService> logger, IObjectMapper objectMapper, IClusterClient clusterClient,
        INESTRepository<CAHolderIndex, Guid> holderRepository, IGraphQLHelper graphQlHelper)
    {
        _freeMintRepository = freeMintRepository;
        _logger = logger;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _holderRepository = holderRepository;
        _graphQlHelper = graphQlHelper;
    }

    public async Task MintAsync(FreeMintEto eventData)
    {
        try
        {
            _logger.LogInformation("[FreeMint] begin handle mint event.");
            // save in es
            var index = await _freeMintRepository.GetAsync(eventData.ConfirmInfo.ItemId);
            if (index == null)
            {
                index = new FreeMintIndex
                {
                    CreateTime = DateTime.UtcNow,
                    UpdateTime = DateTime.UtcNow,
                    Id = eventData.ConfirmInfo.ItemId
                };
                _objectMapper.Map(eventData.ConfirmInfo, index);
                index.CollectionInfo =
                    _objectMapper.Map<FreeMintCollectionInfo, CollectionInfo>(eventData.CollectionInfo);
                await _freeMintRepository.AddOrUpdateAsync(index);
            }
            else
            {
                _objectMapper.Map(index, eventData.ConfirmInfo);
                index.UpdateTime = DateTime.UtcNow;
            }
            // send transaction
            // save transaction info into index

            // test
            // send transaction
            var tranId = await CreateNftItem(eventData.ConfirmInfo.Name, eventData.ConfirmInfo.TokenId,
                index.CollectionInfo.CollectionName, "", eventData.ConfirmInfo.ImageUrl);

            index.TransactionInfos.Add(new MintTransactionInfo()
            {
                BeginTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                BlockTime = 1720454400,
                TransactionId = tranId,
                TransactionResult = "SUCCESS"
            });

            var holder = await GetCaHolderAsync(eventData.UserId);

            var gudto = await GetCaHolderInfoAsync(holder.CaHash);
            await Issue($"{index.CollectionInfo.CollectionName}-{eventData.ConfirmInfo.TokenId}",
                gudto.CaHolderInfo.First().CaAddress);

            var grain = _clusterClient.GetGrain<IFreeMintGrain>(eventData.UserId);
            var changeResult = await grain.ChangeMintStatus(index.Id, FreeMintStatus.SUCCESS);

            index.Status = FreeMintStatus.SUCCESS.ToString();
            await _freeMintRepository.AddOrUpdateAsync(index);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[FreeMint] error");
        }

        // how to handle transactinoInfo
        // success
        // save 
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


    public async Task<string> CreateNftItem(string tokenName, string tokenId, string collectionSymbol, string toAddress,
        string img)
    {
        var address = "DkEdTnymgzVqHmLcGWXiZZuA2A1MeRvC6728BN8yvdGJP7qpC"; //能issue的地址
        var createInput = new CreateInput
        {
            Symbol = $"{collectionSymbol}-{tokenId}",
            TokenName = tokenName,
            TotalSupply = 1,
            Decimals = 0, // 设置为0
            Issuer = Address.FromBase58(address), //能issue的地址
            IssueChainId = ChainHelper.ConvertBase58ToChainId("tDVW"), // Issue给侧链地址
            IsBurnable = true,
            Owner = Address.FromBase58("2ooUsE2vMmp4rBUuVNfH2yGboWkubrH7nd6Wfa8u7CgvRNb9XQ"), //能issue的地址
            ExternalInfo = new ExternalInfo()
            {
                Value =
                {
                    {
                        "__nft_image_url", img
                    }
                }
            }
        };

        var client = new AElfClient("https://tdvw-test-node.aelf.io"); //chain address 
        await client.IsConnectedAsync();


        var transaction =
            await client.GenerateTransactionAsync("DkEdTnymgzVqHmLcGWXiZZuA2A1MeRvC6728BN8yvdGJP7qpC",
                "ASh2Wt7nSEmYqnGxPPzp4pnVDU4uhj1XW9Se5VeZcX2UDdyjx",
                "Create", createInput); // sender address, token address, method name, param

        var txWithSign = client.SignTransaction("3a3bf1c63ae8bcc855890e9b09585b93d18a3402d84b47102fd53b4a5b78dcac",
            transaction); //adress’s private key

        var result = await client.SendTransactionAsync(new SendTransactionInput()
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });

        return result.TransactionId;
    }

    public async Task Issue(string symbol, string to)
    {
        await Task.Delay(8000);
        var client = new AElfClient("https://tdvw-test-node.aelf.io"); //chain address 
        await client.IsConnectedAsync();

        var input = new IssueInput
        {
            Symbol = symbol,
            Amount = 1,
            To = Address.FromBase58(to)
        };
        var transaction =
            await client.GenerateTransactionAsync("DkEdTnymgzVqHmLcGWXiZZuA2A1MeRvC6728BN8yvdGJP7qpC",
                "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
                "Issue", input); // sender address, token address, method name, param

        var txWithSign = client.SignTransaction("3a3bf1c63ae8bcc855890e9b09585b93d18a3402d84b47102fd53b4a5b78dcac",
            transaction);

        var result = await client.SendTransactionAsync(new SendTransactionInput()
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });
    }
}