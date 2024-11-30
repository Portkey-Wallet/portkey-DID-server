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
using CAServer.Commons;
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
            _logger.LogDebug("[AutoReceive] Processing chain: {chainId}", chain.Key);
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
        _logger.LogInformation("[AutoReceive] transfer from grain count:{0}, chainId:{1}", transfers.Count, chainId);

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
                var queryTransfers = list.CaHolderTransactionInfo.Data.Where(t => t.TransferInfo != null).Select(tx =>
                    new CrossChainTransferDto
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
                _logger.LogInformation(
                    "[AutoReceive] Processed height, chainId:{chainId}, startHeight:{startHeight}, endHeight:{endHeight}",
                    chainId, startHeight, endHeight);

                if (transfers.Count > MaxTransferQueryCount || endHeight == indexedHeight - _indexOptions.IndexSafe)
                {
                    break;
                }

                startHeight = endHeight + 1;
                endHeight = Math.Min(startHeight + MaxTransferQueryCount - 1, indexedHeight - _indexOptions.IndexSafe);
            }
        }

        _logger.LogInformation("[AutoReceive] transfer total count:{0}, chianId:{1}", transfers.Count, chainId);
        return transfers;
    }


    private async Task HandleTransferTransactionsAsync(List<CrossChainTransferDto> transfers)
    {
        foreach (var transfer in transfers)
        {
            try
            {
                _logger.LogInformation(
                    "[AutoReceive] Handle transfer tx, fromChainId: {0}, transferTransactionId:{1}, status:{2}",
                    transfer.FromChainId, transfer.TransferTransactionId, transfer.Status);
                if (!await ValidateTransactionAsync(transfer))
                {
                    _logger.LogWarning(
                        "[AutoReceive] Wrong cross chain transaction, fromChainId: {0}, transferTransactionId:{1}",
                        transfer.FromChainId, transfer.TransferTransactionId);
                    continue;
                }

                var blockHeight = await _graphQlProvider.GetIndexBlockHeightAsync(transfer.ToChainId);
                var endHeight = blockHeight - _indexOptions.IndexSafe;
                var receivedTx = await _graphQlProvider.GetReceiveTransactionAsync(transfer.ToChainId,
                    transfer.TransferTransactionId, endHeight);
                if (receivedTx != null)
                {
                    transfer.Status = CrossChainStatus.Confirmed;
                    _logger.LogWarning("[AutoReceive] Already received, transferTransactionId: {0}",
                        transfer.TransferTransactionId);
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
                _logger.LogError(e, "[AutoReceive] Cross chain transfer auto receive failed, message:{0}, stack:{1}",
                    e.Message, e.StackTrace ?? "-");
            }
        }
    }

    private async Task<bool> ValidateTransactionAsync(CrossChainTransferDto transfer)
    {
        if (transfer.FromChainId == transfer.ToChainId || !_chainOptions.ChainInfos.ContainsKey(transfer.FromChainId) ||
            !_chainOptions.ChainInfos.ContainsKey(transfer.ToChainId))
        {
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
            _logger.LogWarning(
                "[AutoReceive] Receive transaction failed, receiveTransactionId:{0}, transferTransactionId:{1}, errorMsg:{2}, retryTimes:{3}",
                transfer.ReceiveTransactionId, transfer.TransferTransactionId, txResult.Error, transfer.RetryTimes);
            if (transfer.RetryTimes < MaxRetryTimes)
            {
                var txId = await SendReceiveTransactionAsync(transfer);
                transfer.ReceiveTransactionId = txId;
                transfer.RetryTimes += 1;
            }
            else
            {
                _logger.LogWarning("[AutoReceive] retry to many times, transferTransactionId:{0}",
                    transfer.TransferTransactionId);
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
        _logger.LogInformation(
            "[AutoReceive] CheckTransactionConfirmResult transferTransactionId:{0}, lastIrreversibleBlockHeight:{1}, receiveTransactionBlockHeight:{2}, status:{3}",
            transfer.TransferTransactionId, chainStatus.LastIrreversibleBlockHeight,
            transfer.ReceiveTransactionBlockHeight, transfer.Status);
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
        try
        {
            var txResult =
                await _contractProvider.GetTransactionResultAsync(transfer.FromChainId,
                    transfer.TransferTransactionId);
            var parentHeight = txResult.BlockNumber;
            var paramsJson = JsonNode.Parse(txResult.Transaction.Params);

            string transferTransactionId;
            var transaction = new Transaction();
            if (txResult.Transaction.MethodName == AElfContractMethodName.ManagerForwardCall &&
                paramsJson["methodName"].ToString() == CommonConstant.InlineCrossChainTransferMethodName)
            {
                transaction = GetInlineCrossChainTransferTransaction(txResult);
                transferTransactionId = transaction.GetHash().ToHex();
            }
            else
            {
                transaction = GetCrossChainTransferTransaction(paramsJson, txResult);
                transferTransactionId = transfer.TransferTransactionId;
            }

            var merklePath = await GetMerklePathAsync(transfer.FromChainId, transferTransactionId);
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

            _logger.LogInformation(
                "[AutoReceive] Send receive transaction finished, fromChainId: {0}, transferTransactionId:{1}, status:{2}, txId:{3}",
                transfer.FromChainId, transfer.TransferTransactionId, transfer.Status, txId);

            return txId;
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "[AutoReceive] Send receive transaction error, fromChainId: {0}, transferTransactionId:{1}, errMsg:{2}, stack:{3}",
                transfer.FromChainId, transfer.TransferTransactionId, e.Message, e.StackTrace ?? "-");
            throw;
        }
    }

    private Transaction GetCrossChainTransferTransaction(JsonNode paramsJson, TransactionResultDto txResult)
    {
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

        return new Transaction
        {
            From = Address.FromBase58(txResult.Transaction.From),
            To = Address.FromBase58(txResult.Transaction.To),
            Params = param.ToByteString(),
            Signature = ByteString.FromBase64(txResult.Transaction.Signature),
            MethodName = txResult.Transaction.MethodName,
            RefBlockNumber = txResult.Transaction.RefBlockNumber,
            RefBlockPrefix = ByteString.FromBase64(txResult.Transaction.RefBlockPrefix)
        };
    }

    private Transaction GetInlineCrossChainTransferTransaction(TransactionResultDto txResult)
    {
        var indexed = txResult.Logs.First(p => p.Name.Equals(InlineTransactionCreated.Descriptor.Name)).Indexed[0];
        var inlineTransactionCreated = InlineTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed));
        return inlineTransactionCreated.Transaction;
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