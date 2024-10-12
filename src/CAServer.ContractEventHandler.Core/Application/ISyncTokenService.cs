using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Options;
using CAServer.Signature.Provider;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Hash = AElf.Types.Hash;
using MerklePath = AElf.Types.MerklePath;
using MerklePathNode = AElf.Types.MerklePathNode;
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
    private readonly INESTRepository<FreeMintNftSyncIndex, string> _freeMintNftSyncRepository;
    private readonly IDistributedCache<ChainHeightCache> _distributedCache;
    private readonly IndexOptions _indexOptions;

    public SyncTokenService(ISignatureProvider signatureProvider,
        IOptionsSnapshot<ChainOptions> chainOptions,
        IOptionsSnapshot<ContractServiceOptions> contractGrainOptions,
        IOptionsSnapshot<ContractOptions> contractOptions,
        IOptionsSnapshot<IndexOptions> indexOptions,
        IOptionsSnapshot<PayRedPackageAccount> packageAccount, ILogger<SyncTokenService> logger,
        INESTRepository<FreeMintNftSyncIndex, string> freeMintNftSyncRepository,
        IDistributedCache<ChainHeightCache> distributedCache)
    {
        _signatureProvider = signatureProvider;
        _logger = logger;
        _freeMintNftSyncRepository = freeMintNftSyncRepository;
        _distributedCache = distributedCache;
        _chainOptions = chainOptions.Value;
        _contractServiceOptions = contractGrainOptions.Value;
        _packageAccount = packageAccount.Value;
        _contractOptions = contractOptions.Value;
        _indexOptions = indexOptions.Value;
    }

    public async Task SyncTokenToOtherChainAsync(string chainId, string symbol)
    {
        _logger.LogInformation(
            "SyncTokenToOtherChainAsync [SyncToken] Begin to sync token, fromChainId:{chainId}, toChainId:{toChainId}, symbol:{symbol}", chainId,
            CommonConstant.MainChainId, symbol);
        var nftSyncIndex = await SaveSyncRecordAsync(chainId, symbol);
        try
        {
            var from = _packageAccount.getOneAccountRandom();
            var crossChainCreateTokenInput = await GetCrossChainCreateTokenInput(chainId, symbol, from);

            // sync token info to main chain
            var toChainInfo = _chainOptions.ChainInfos.GetOrDefault(CommonConstant.MainChainId);
            var transactionInfo = await SendTransactionAsync(CommonConstant.MainChainId, crossChainCreateTokenInput,
                from,
                toChainInfo.TokenContractAddress, "CrossChainCreateToken");

            if (transactionInfo == null || transactionInfo.TransactionResultDto == null)
            {
                await SaveSyncRecordResultAsync(nftSyncIndex, null);
                _logger.LogInformation("SyncTokenToOtherChainAsync [SyncToken] sync token fail, toChainId:{chainId}, symbol:{symbol}",
                    CommonConstant.MainChainId,
                    symbol);
            }

            _logger.LogInformation(
                "SyncTokenToOtherChainAsync [SyncToken] End to sync token, toChainId:{chainId}, symbol:{symbol}, transactionId:{transactionId}, transactionResult:{transactionResult}, errorMessage:{message}",
                CommonConstant.MainChainId,
                symbol, transactionInfo.TransactionResultDto.TransactionId, transactionInfo.TransactionResultDto.Status,
                transactionInfo.TransactionResultDto.Error ?? "-");
            await SaveSyncRecordResultAsync(nftSyncIndex, transactionInfo.TransactionResultDto);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "[SyncToken] sync token error, fromChainId:{chainId}, toChainId:{toChainId}, symbol:{symbol}", chainId,
                CommonConstant.MainChainId, symbol);
            await SaveSyncRecordResultAsync(nftSyncIndex, null, e.Message);
        }
    }

    private async Task<CrossChainCreateTokenInput> GetCrossChainCreateTokenInput(string chainId, string symbol,
        string from)
    {
        var client = new AElfClient(_chainOptions.ChainInfos.GetOrDefault(chainId).BaseUrl);
        await client.IsConnectedAsync();

        var tokenInfo = await GetTokenInfo(chainId, symbol);
        _logger.LogInformation("[SyncToken] symbol:{symbol}", tokenInfo.Symbol);
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
            TransactionBytes = tokenValidationTransaction.Transaction.ToByteString(),
            MerklePath = merklePath
        };

        var getCrossChainMerkleProofContextInput = new Int64Value
        {
            Value = tokenValidationTransaction.TransactionResultDto.BlockNumber
        };

        var crossChainMerkleProofContext = await CallTransactionAsync<AElf.Standards.ACS7.CrossChainMerkleProofContext>(
            "GetBoundParentChainHeightAndMerklePathByHeight", getCrossChainMerkleProofContextInput,
            _chainOptions.ChainInfos[chainId].CrossChainContractAddress, chainId);

        crossChainCreateTokenInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
            .MerklePathFromParentChain.MerklePathNodes);
        crossChainCreateTokenInput.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;

        _logger.LogInformation(
            "[SyncToken] crossChainMerkleProofContext, chainId:{chainId}, crossChainContractAddress:{crossChainContractAddress}, parentChainHeight:{parentChainHeight}",
            chainId, _chainOptions.ChainInfos[chainId].CrossChainContractAddress,
            crossChainCreateTokenInput.ParentChainHeight);
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

    private async Task MainChainCheckMainChainBlockIndexAsync(string chainId, string otherChainId, long txHeight)
    {
        var checkResult = false;
        var mainHeight = long.MaxValue;
        var time = 0;

        while (!checkResult && time < _indexOptions.IndexTimes)
        {
            var cache = await _distributedCache.GetAsync(nameof(ChainHeightCache));
            _logger.LogInformation(
                "txHeight:{txHeight}, indexMainChainBlock:{indexMainChainBlock}, mainHeight:{mainHeight}, indexMainChainHeight:{indexMainChainHeight}",
                txHeight, cache.SideChainIndexHeight, cache.MainChainBlockHeight, cache.ParentChainHeight);

            var sideChainIndexHeight = cache.SideChainIndexHeight;
            if (sideChainIndexHeight < txHeight)
            {
                await Task.Delay(_indexOptions.IndexDelay);
                time++;
                _logger.LogInformation(
                    "[SyncToken] valid txHeight:{txHeight}, sideChainIndexHeight: {sideChainIndexHeight}, time:{time}",
                    txHeight,
                    sideChainIndexHeight, time);
                continue;
            }

            mainHeight = mainHeight == long.MaxValue
                ? cache.MainChainBlockHeight
                : mainHeight;

            var indexMainChainHeight = cache.ParentChainHeight;
            _logger.LogInformation(
                "[SyncToken] valid indexMainChainHeight:{indexMainChainHeight}, mainHeight: {mainHeight}",
                indexMainChainHeight, mainHeight);

            checkResult = indexMainChainHeight > mainHeight;
            await Task.Delay(_indexOptions.IndexDelay);
        }

        CheckIndexBlockHeightResult(checkResult, time);
    }

    private void CheckIndexBlockHeightResult(bool result, int time)
    {
        if (!result && time == _indexOptions.IndexTimes)
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

    private async Task<FreeMintNftSyncIndex> SaveSyncRecordAsync(string chainId, string symbol)
    {
        var nftSyncIndex = new FreeMintNftSyncIndex()
        {
            Id = Guid.NewGuid().ToString(),
            BeginTime = DateTime.UtcNow,
            Symbol = symbol,
            ChainId = chainId
        };
        await _freeMintNftSyncRepository.AddOrUpdateAsync(nftSyncIndex);
        return nftSyncIndex;
    }

    private async Task SaveSyncRecordResultAsync(FreeMintNftSyncIndex nftSyncIndex,
        TransactionResultDto resultDto, string errorMessage = "")
    {
        if (resultDto == null)
        {
            nftSyncIndex.ErrorMessage = errorMessage;
            nftSyncIndex.EndTime = DateTime.UtcNow;
            await _freeMintNftSyncRepository.AddOrUpdateAsync(nftSyncIndex);
        }

        nftSyncIndex.TransactionId = resultDto.TransactionId;
        nftSyncIndex.BlockNumber = resultDto.BlockNumber;
        nftSyncIndex.TransactionResult = resultDto.Status;
        nftSyncIndex.ErrorMessage = resultDto.Error;
        nftSyncIndex.EndTime = DateTime.UtcNow;
        await _freeMintNftSyncRepository.AddOrUpdateAsync(nftSyncIndex);
    }
}