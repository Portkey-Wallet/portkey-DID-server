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
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Options;
using CAServer.Signature.Provider;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using TokenInfo = AElf.Contracts.MultiToken.TokenInfo;

namespace CAServer.ContractEventHandler.Core.Application;

public interface ISyncTokenService
{
    Task SyncTokenToOtherChainAsync(string chainId, string symbol);
}

public class SyncTokenService : ISyncTokenService, ISingletonDependency
{
    private readonly ISignatureProvider _signatureProvider;
    private readonly ChainOptions _chainOptions;
    private readonly ContractServiceOptions _contractServiceOptions;
    private readonly PayRedPackageAccount _packageAccount;
    private readonly ILogger<SyncTokenService> _logger;
    private readonly ContractOptions _contractOptions;

    public SyncTokenService(ISignatureProvider signatureProvider,
        IOptionsSnapshot<ChainOptions> chainOptions,
        IOptionsSnapshot<ContractServiceOptions> contractGrainOptions,
        IOptionsSnapshot<ContractOptions> contractOptions,
        IOptionsSnapshot<PayRedPackageAccount> packageAccount, ILogger<SyncTokenService> logger)
    {
        _signatureProvider = signatureProvider;
        _logger = logger;
        _chainOptions = chainOptions.Value;
        _contractServiceOptions = contractGrainOptions.Value;
        _packageAccount = packageAccount.Value;
        _contractOptions = contractOptions.Value;
    }

    public async Task SyncTokenToOtherChainAsync(string chainId, string symbol)
    {
        _logger.LogInformation("[SyncToken] Begin to sync token, chainId:{chainId}, symbol:{symbol}", chainId, symbol);
        var from = _packageAccount.getOneAccountRandom();
        var crossChainCreateTokenInput = await GetCrossChainCreateTokenInput(chainId, symbol, from);
        var chainInfo = _chainOptions.ChainInfos.GetOrDefault(chainId);
        var transactionInfo = await SendTransactionAsync(chainId, crossChainCreateTokenInput, from,
            chainInfo.CrossChainContractAddress, "CrossChainCreateToken");
        if (transactionInfo == null || transactionInfo.TransactionResultDto == null)
        {
            _logger.LogInformation("[SyncToken] sync token error, chainId:{chainId}, symbol:{symbol}", chainId, symbol);
        }

        _logger.LogInformation(
            "[SyncToken] End to sync token, chainId:{chainId}, symbol:{symbol}, transactionResult:{transactionResult}",
            chainId,
            symbol, JsonConvert.SerializeObject(transactionInfo.TransactionResultDto));
    }

    private async Task<CrossChainCreateTokenInput> GetCrossChainCreateTokenInput(string chainId, string symbol,
        string from)
    {
        var client = new AElfClient(_chainOptions.ChainInfos.GetOrDefault(chainId).BaseUrl);
        await client.IsConnectedAsync();

        var tokenInfo = await GetTokenInfo(chainId, symbol);
        _logger.LogInformation("[SyncToken] tokenInfo:{tokenInfo}", JsonConvert.SerializeObject(tokenInfo));
        var tokenValidationTransaction = await CreateTokenInfoValidationTransaction(chainId, from, tokenInfo);

        var merklePathDto =
            await client.GetMerklePathByTransactionIdAsync(
                tokenValidationTransaction.TransactionResultDto.TransactionId);

        var merklePath = new MerklePath();
        foreach (var node in merklePathDto.MerklePathNodes)
        {
            merklePath.MerklePathNodes.Add(new MerklePathNode
            {
                Hash = new Hash { Value = Hash.LoadFromHex(node.Hash).Value },
                IsLeftChildNode = node.IsLeftChildNode
            });
        }

        var otherChainId = _chainOptions.ChainInfos.Keys.FirstOrDefault(t => t != chainId);
        await MainChainCheckMainChainBlockIndexAsync(chainId, otherChainId,
            tokenValidationTransaction.TransactionResultDto.BlockNumber);

        var crossChainCreateTokenInput = new CrossChainCreateTokenInput
        {
            FromChainId = ChainHelper.ConvertBase58ToChainId(chainId),
            ParentChainHeight = tokenValidationTransaction.TransactionResultDto.BlockNumber,
            TransactionBytes = tokenValidationTransaction.Transaction.ToByteString(),
            MerklePath = merklePath
        };

        return crossChainCreateTokenInput;
    }

    private async Task<TokenInfo> GetTokenInfo(string chainId, string symbol)
    {
        var getTokenInput = new GetTokenInfoInput
        {
            Symbol = symbol
        };

        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainInfo))
        {
            return null;
        }

        return await CallTransactionAsync<TokenInfo>("GetTokenInfo", getTokenInput, chainInfo.TokenContractAddress,
            chainId);
    }

    private async Task<long> GetIndexHeightFromMainChainAsync(string chainId, int sideChainId)
    {
        var param = new Int32Value
        {
            Value = sideChainId
        };
        var value = await CallTransactionAsync<Int64Value>(MethodName.GetSideChainHeight, param,
            _chainOptions.ChainInfos.GetOrDefault(chainId).CrossChainContractAddress, chainId);

        return value.Value;
    }

    private async Task MainChainCheckMainChainBlockIndexAsync(string chainId, string otherChainId, long txHeight)
    {
        var fromChain = ChainHelper.ConvertBase58ToChainId(chainId);
        var checkResult = false;
        var time = 0;

        while (!checkResult && time < 40)
        {
            var indexMainChainBlock = await GetIndexHeightFromMainChainAsync(otherChainId, fromChain);
            _logger.LogInformation($"valid txHeight:{txHeight}, indexMainChainBlock: {indexMainChainBlock}");
            if (indexMainChainBlock < txHeight)
            {
                await Task.Delay(10000);
                time++;
                continue;
            }

            checkResult = true;
        }

        CheckIndexBlockHeightResult(checkResult, time);
    }

    private void CheckIndexBlockHeightResult(bool result, int time)
    {
        if (!result && time == 40)
        {
            throw new Exception(LoggerMsg.IndexTimeoutError);
        }
    }

    private async Task<TransactionInfoDto> CreateTokenInfoValidationTransaction(string chainId, string from,
        TokenInfo createdTokenInfo)
    {
        var input = new ValidateTokenInfoExistsInput
        {
            TokenName = createdTokenInfo.TokenName,
            Symbol = createdTokenInfo.Symbol,
            Decimals = createdTokenInfo.Decimals,
            Issuer = createdTokenInfo.Issuer,
            Owner = createdTokenInfo.Owner,
            IsBurnable = createdTokenInfo.IsBurnable,
            TotalSupply = createdTokenInfo.TotalSupply,
            IssueChainId = createdTokenInfo.IssueChainId
        };
        if (createdTokenInfo.ExternalInfo != null)
        {
            input.ExternalInfo.Add(createdTokenInfo.ExternalInfo.Value);
        }

        return await SendTransactionAsync(chainId, input, from,
            _chainOptions.ChainInfos.GetOrDefault(chainId).TokenContractAddress, "ValidateTokenInfoExists");
    }

    private async Task<T> CallTransactionAsync<T>(string methodName, IMessage param, string contractAddress,
        string chainId) where T : class, IMessage<T>, new()
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainInfo))
        {
            return null;
        }

        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();

        string addressFromPrivateKey = client.GetAddressFromPrivateKey(_contractOptions.CommonPrivateKeyForCallTx);

        var transaction =
            await client.GenerateTransactionAsync(addressFromPrivateKey, contractAddress, methodName, param);
        _logger.LogDebug("[SyncToken] Call tx methodName is: {methodName} param is: {transaction}", methodName,
            transaction);

        var txWithSign = client.SignTransaction(_contractOptions.CommonPrivateKeyForCallTx, transaction);

        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });

        var value = new T();
        value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));

        return value;
    }

    private async Task<TransactionInfoDto> SendTransactionAsync(string chainId, IMessage param,
        string from, string contractAddress, string methodName)
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
                "[SyncToken] Get Address From PubKey, ownAddress：{ownAddress}, ContractAddress: {ContractAddress} ,methodName:{methodName}",
                ownAddress, contractAddress, methodName);

            var transaction =
                await client.GenerateTransactionAsync(ownAddress, contractAddress, methodName,
                    param);

            var txWithSign = await _signatureProvider.SignTxMsg(from, transaction.GetHash().ToHex());
            transaction.Signature = ByteStringHelper.FromHexString(txWithSign);

            var result = await client.SendTransactionAsync(new SendTransactionInput
            {
                RawTransaction = transaction.ToByteArray().ToHex()
            });
            _logger.LogInformation("[SyncToken] Send transaction, transactionId: {transactionId}",
                result.TransactionId);

            await Task.Delay(_contractServiceOptions.Delay);

            var transactionResult = await client.GetTransactionResultAsync(result.TransactionId);
            _logger.LogInformation(
                "[SyncToken] query transactionResult, transactionId:{transactionId}, transaction status:{status}",
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
            _logger.LogError(e, "SyncToken transaction error: {param}", JsonConvert.SerializeObject(param));
            return null;
        }
    }
}