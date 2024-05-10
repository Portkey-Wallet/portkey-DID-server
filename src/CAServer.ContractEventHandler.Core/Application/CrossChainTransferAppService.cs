using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.Standards.ACS7;
using AElf.Types;
using CAServer.Grains.Grain.CrossChain;
using CAServer.Grains.State.CrossChain;
using CAServer.Signature;
using CAServer.Signature.Provider;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.DependencyInjection;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace CAServer.ContractEventHandler.Core.Application;

public interface ICrossChainTransferAppService
{
    Task AutoReceiveAsync();
    Task ResetRetryTimesAsync();
}

public class CrossChainTransferAppService : ICrossChainTransferAppService, ITransientDependency
{
    private readonly ChainOptions _chainOptions;
    private readonly IndexOptions _indexOptions;
    private readonly IContractProvider _contractProvider;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IClusterClient _clusterClient;
    private readonly CrossChainOptions _crossChainOptions;
    private readonly ILogger<CrossChainTransferAppService> _logger;
    private readonly ISignatureProvider _signatureProvider;


    private const int MaxTransferQueryCount = 100;
    private const int MaxRetryTimes = 5;

    public CrossChainTransferAppService(IContractProvider contractProvider, IOptionsSnapshot<ChainOptions> chainOptions,
        ILogger<CrossChainTransferAppService> logger, IGraphQLProvider graphQlProvider,
        IClusterClient clusterClient, IOptions<IndexOptions> indexOptions,
        IOptionsSnapshot<CrossChainOptions> crossChainOptions, ISignatureProvider signatureProvider)
    {
        _contractProvider = contractProvider;
        _chainOptions = chainOptions.Value;
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _clusterClient = clusterClient;
        _indexOptions = indexOptions.Value;
        _crossChainOptions = crossChainOptions.Value;
        _signatureProvider = signatureProvider;
    }

    public async Task AutoReceiveAsync()
    {
        foreach (var chain in _chainOptions.ChainInfos)
        {
            _logger.LogDebug($"Processing chain: {chain.Key}");
            var txs = await GetToReceiveTransactionsAsync(chain.Value.ChainId);
            await HandleTransferTransactionsAsync(txs);
        }
    }

    public async Task ResetRetryTimesAsync()
    {
        foreach (var chain in _chainOptions.ChainInfos)
        {
            var grain = _clusterClient.GetGrain<ICrossChainTransferGrain>(chain.Key);
            var transfers = (await grain.GetUnFinishedTransfersAsync()).Data;
            foreach (var transfer in transfers)
            {
                transfer.RetryTimes = 0;
                await UpdateTransferAsync(transfer);
            }
        }
    }

    private async Task<List<CrossChainTransferDto>> GetToReceiveTransactionsAsync(string chainId)
    {
        var grain = _clusterClient.GetGrain<ICrossChainTransferGrain>(chainId);
        
        var transfers = (await grain.GetUnFinishedTransfersAsync()).Data;
        
        transfers = transfers.Where(o => o.RetryTimes < MaxRetryTimes).ToList();

        if (transfers.Count < MaxTransferQueryCount)
        {
            var latestProcessedHeight = (await grain.GetLastedProcessedHeightAsync()).Data;
            if (latestProcessedHeight < _crossChainOptions.AutoReceiveStartHeight[chainId] - 1)
            {
                latestProcessedHeight = _crossChainOptions.AutoReceiveStartHeight[chainId] - 1;
            }

            var indexedHeight = await _graphQlProvider.GetIndexBlockHeightAsync(chainId);
            var startHeight = latestProcessedHeight + 1;
            var endHeight = Math.Min(startHeight + MaxTransferQueryCount - 1, indexedHeight - _indexOptions.IndexSafe);

            while (true)
            {
                var list = await _graphQlProvider.GetToReceiveTransactionsAsync(chainId, startHeight, endHeight);
                var queryTransfers = list.CaHolderTransactionInfo.Data.Select(tx => new CrossChainTransferDto
                {
                    Id = tx.TransactionId,
                    FromChainId = tx.TransferInfo.FromChainId,
                    ToChainId = tx.TransferInfo.ToChainId,
                    TransferTransactionId = tx.TransactionId,
                    TransferTransactionHeight = tx.BlockHeight,
                    TransferTransactionBlockHash = tx.BlockHash,
                    Status = CrossChainStatus.Indexing
                }).ToList();

                await grain.AddTransfersAsync(endHeight, queryTransfers);
                transfers.AddRange(queryTransfers);
                _logger.LogDebug($"Processed height: {chainId}, {endHeight}");

                if (transfers.Count > MaxTransferQueryCount || endHeight == indexedHeight - _indexOptions.IndexSafe)
                {
                    break;
                }

                startHeight = endHeight + 1;
                endHeight = Math.Min(startHeight + MaxTransferQueryCount - 1, indexedHeight - _indexOptions.IndexSafe);
            }
        }

        return transfers;
    }


    private async Task HandleTransferTransactionsAsync(List<CrossChainTransferDto> transfers)
    {
        foreach (var transfer in transfers)
        {
            try
            {
                _logger.LogDebug($"Handle transfer tx: {transfer.FromChainId}, {transfer.TransferTransactionId}");

                if (!await ValidateTransactionAsync(transfer))
                {
                    continue;
                }

                var blockHeight = await _graphQlProvider.GetIndexBlockHeightAsync(transfer.ToChainId);
                var endHeight = blockHeight - _indexOptions.IndexSafe;
                var receivedTx = await _graphQlProvider.GetReceiveTransactionAsync(transfer.ToChainId,
                    transfer.TransferTransactionId, endHeight);
                if (receivedTx != null)
                {
                    transfer.Status = CrossChainStatus.Confirmed;
                    await UpdateTransferAsync(transfer);
                    continue;
                }

                switch (transfer.Status)
                {
                    case CrossChainStatus.Indexing:
                        await HandleTransferTransactionAsync(transfer);
                        break;
                    case CrossChainStatus.Receiving:
                        await CheckTransactionResultAsync(transfer);
                        break;
                    case CrossChainStatus.Received:
                        await CheckTransactionConfirmResultAsync(transfer);
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Cross chain transfer auto receive failed.");
                throw;
            }
        }
    }

    private async Task<bool> ValidateTransactionAsync(CrossChainTransferDto transfer)
    {
        if (transfer.FromChainId == transfer.ToChainId || !_chainOptions.ChainInfos.ContainsKey(transfer.FromChainId) ||
            !_chainOptions.ChainInfos.ContainsKey(transfer.ToChainId))
        {
            _logger.LogError($"Wrong cross chain transaction: {transfer.TransferTransactionId}");
            transfer.RetryTimes = MaxRetryTimes;
            await UpdateTransferAsync(transfer);

            return false;
        }

        return true;
    }

    private async Task CheckTransactionResultAsync(CrossChainTransferDto transfer)
    {
        var txResult =
            await _contractProvider.GetTransactionResultAsync(transfer.ToChainId,
                transfer.ReceiveTransactionId);
        if (txResult.Status != TransactionResultStatus.Mined.ToString().ToUpper() &&
            txResult.Status != TransactionResultStatus.Pending.ToString().ToUpper())
        {
            _logger.LogDebug($"Receive transaction {transfer.ReceiveTransactionId} failed: {txResult.Error}");
            if (transfer.RetryTimes < MaxRetryTimes)
            {
                var txId = await SendReceiveTransactionAsync(transfer);
                transfer.ReceiveTransactionId = txId;
                transfer.RetryTimes += 1;
            }
            else
            {
                _logger.LogWarning($"Transaction {transfer.TransferTransactionId} retry to many times");
            }

            await UpdateTransferAsync(transfer);
        }
        else if (txResult.Status == TransactionResultStatus.Mined.ToString())
        {
            transfer.Status = CrossChainStatus.Received;
            transfer.ReceiveTransactionBlockHash = txResult.BlockHash;
            transfer.ReceiveTransactionBlockHeight = txResult.BlockNumber;

            await UpdateTransferAsync(transfer);
        }
    }

    private async Task CheckTransactionConfirmResultAsync(CrossChainTransferDto transfer)
    {
        var chainStatus = await _contractProvider.GetChainStatusAsync(transfer.ToChainId);
        if (chainStatus.LastIrreversibleBlockHeight >= transfer.ReceiveTransactionBlockHeight)
        {
            var block = await _contractProvider.GetBlockByHeightAsync(transfer.ToChainId,
                transfer.ReceiveTransactionBlockHeight);
            if (block.BlockHash != transfer.ReceiveTransactionBlockHash)
            {
                transfer.ReceiveTransactionId = null;
                transfer.Status = CrossChainStatus.Indexing;
            }
            else
            {
                transfer.Status = CrossChainStatus.Confirmed;
            }

            await UpdateTransferAsync(transfer);
        }
    }

    private async Task HandleTransferTransactionAsync(CrossChainTransferDto transfer)
    {
        if (transfer.FromChainId == CAServerConsts.AElfMainChainId)
        {
            var indexHeight = await _contractProvider.GetIndexHeightFromSideChainAsync(transfer.ToChainId);
            if (indexHeight < transfer.TransferTransactionHeight)
            {
                return;
            }

            var txId = await SendReceiveTransactionAsync(transfer);
            transfer.Status = CrossChainStatus.Receiving;
            transfer.ReceiveTransactionId = txId;
            await UpdateTransferAsync(transfer);
        }
        else
        {
            var fromChain = ChainHelper.ConvertBase58ToChainId(transfer.FromChainId);

            if (transfer.MainChainIndexHeight != 0)
            {
                var indexHeight = await _contractProvider.GetIndexHeightFromSideChainAsync(transfer.FromChainId);
                if (indexHeight < transfer.MainChainIndexHeight)
                {
                    return;
                }

                if (transfer.ToChainId != CAServerConsts.AElfMainChainId)
                {
                    indexHeight = await _contractProvider.GetIndexHeightFromSideChainAsync(transfer.ToChainId);
                    if (indexHeight < transfer.MainChainIndexHeight)
                    {
                        return;
                    }
                }

                var txId = await SendReceiveTransactionAsync(transfer);
                transfer.Status = CrossChainStatus.Receiving;
                transfer.ReceiveTransactionId = txId;
                await UpdateTransferAsync(transfer);
            }
            else
            {
                var indexHeight =
                    await _contractProvider.GetIndexHeightFromMainChainAsync(CAServerConsts.AElfMainChainId, fromChain);
                if (indexHeight > transfer.TransferTransactionHeight)
                {
                    transfer.MainChainIndexHeight = indexHeight;
                    await UpdateTransferAsync(transfer);
                }
            }
        }
    }

    private async Task<string> SendReceiveTransactionAsync(CrossChainTransferDto transfer)
    {
        var txResult =
            await _contractProvider.GetTransactionResultAsync(transfer.FromChainId,
                transfer.TransferTransactionId);
        var parentHeight = txResult.BlockNumber;

        var paramsJson = JsonNode.Parse(txResult.Transaction.Params);
        var param = new CrossChainTransferInput
        {
            To = Address.FromBase58(paramsJson["to"].ToString()),
            Amount = long.Parse(paramsJson["amount"].ToString()),
            Symbol = paramsJson["symbol"].ToString(),
            IssueChainId = int.Parse(paramsJson["issueChainId"].ToString()),
            ToChainId = int.Parse(paramsJson["toChainId"].ToString())
        };

        if (paramsJson["memo"] != null)
        {
            param.Memo = paramsJson["memo"].ToString();
        }

        var transaction = new Transaction
        {
            From = Address.FromBase58(txResult.Transaction.From),
            To = Address.FromBase58(txResult.Transaction.To),
            Params = param.ToByteString(),
            Signature = ByteString.FromBase64(txResult.Transaction.Signature),
            MethodName = txResult.Transaction.MethodName,
            RefBlockNumber = txResult.Transaction.RefBlockNumber,
            RefBlockPrefix = ByteString.FromBase64(txResult.Transaction.RefBlockPrefix)
        };

        var merklePath = await GetMerklePathAsync(transfer.FromChainId, transfer.TransferTransactionId);
        if (transfer.FromChainId != CAServerConsts.AElfMainChainId)
        {
            var merkleProofContext =
                await GetBoundParentChainHeightAndMerklePathByHeightAsync(
                    transfer.FromChainId, txResult.BlockNumber);
            parentHeight = merkleProofContext.BoundParentChainHeight;
            merklePath.MerklePathNodes.AddRange(merkleProofContext.MerklePathFromParentChain.MerklePathNodes);
        }

        var txId = await SendCrossChainReceiveTokenAsync(transfer.ToChainId, transfer.FromChainId, parentHeight,
            transaction.ToByteArray().ToHex(), merklePath);
        _logger.LogDebug($"Send transaction {txId} finished");
        return txId;
    }

    private async Task<MerklePath> GetMerklePathAsync(string chainId, string txId)
    {
        var merklePathDto = await GetMerklePathFromChainAsync(chainId, txId);
        var merklePath = new MerklePath();
        foreach (var node in merklePathDto.MerklePathNodes)
        {
            merklePath.MerklePathNodes.Add(new MerklePathNode()
            {
                Hash = new Hash() { Value = Hash.LoadFromHex(node.Hash).Value },
                IsLeftChildNode = node.IsLeftChildNode
            });
        }

        return merklePath;
    }

    private async Task<MerklePathDto> GetMerklePathFromChainAsync(string chainId, string txId)
    {
        var chainInfo = _chainOptions.ChainInfos[chainId];
        var client = new AElfClient(chainInfo.BaseUrl);
        return await client.GetMerklePathByTransactionIdAsync(txId);
    }

    private async Task<CrossChainMerkleProofContext> GetBoundParentChainHeightAndMerklePathByHeightAsync(string chainId,
        long height)
    {
        var chainInfo = _chainOptions.ChainInfos[chainId];
        var client = new AElfClient(chainInfo.BaseUrl);
        var ownAddress = client.GetAddressFromPubKey(chainInfo.PublicKey);

        var param = new Int64Value
        {
            Value = height
        };
        var transaction =
            await client.GenerateTransactionAsync(client.GetAddressFromPubKey(chainInfo.PublicKey),
                chainInfo.CrossChainContractAddress,
                "GetBoundParentChainHeightAndMerklePathByHeight", param);
        var txWithSign = await _signatureProvider.SignTxMsg(ownAddress, transaction.GetHash().ToHex());

        transaction.Signature = ByteStringHelper.FromHexString(txWithSign);

        var transactionResult = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = transaction.ToByteArray().ToHex()
        });

        var result =
            CrossChainMerkleProofContext.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(transactionResult));
        return result;
    }

    private async Task<string> SendCrossChainReceiveTokenAsync(string chainId, string fromChainId,
        long parentChainHeight,
        string transferTransaction, MerklePath merklePath)
    {
        var chainInfo = _chainOptions.ChainInfos[chainId];
        var client = new AElfClient(chainInfo.BaseUrl);
        var ownAddress = client.GetAddressFromPubKey(chainInfo.PublicKey);

        var param = new CrossChainReceiveTokenInput
        {
            MerklePath = merklePath,
            FromChainId = ChainHelper.ConvertBase58ToChainId(fromChainId),
            ParentChainHeight = parentChainHeight,
            TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(transferTransaction)),
        };
        var fromAddress = client.GetAddressFromPubKey(chainInfo.PublicKey);
        var transaction = await client.GenerateTransactionAsync(fromAddress, chainInfo.TokenContractAddress,
            "CrossChainReceiveToken", param);
        var txWithSign = await _signatureProvider.SignTxMsg(ownAddress, transaction.GetHash().ToHex());
        transaction.Signature = ByteStringHelper.FromHexString(txWithSign);

        var result = await client.SendTransactionAsync(new SendTransactionInput
        {
            RawTransaction = transaction.ToByteArray().ToHex()
        });

        return result.TransactionId;
    }

    private async Task UpdateTransferAsync(CrossChainTransferDto transfer)
    {
        var grain = _clusterClient.GetGrain<ICrossChainTransferGrain>(transfer.FromChainId);
        await grain.UpdateTransferAsync(transfer);
    }
}