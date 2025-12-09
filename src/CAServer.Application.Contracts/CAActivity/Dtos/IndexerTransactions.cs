using System.Collections.Generic;
using CAServer.UserAssets.Provider;

namespace CAServer.CAActivity.Provider;

public class IndexerTransactions
{
    public CaHolderTransaction CaHolderTransaction { get; set; }
}

public class TransactionsDto
{
    public CaHolderTransaction TwoCaHolderTransaction { get; set; }
}

public class AutoReceiveTransactions
{
    public CaHolderTransaction AutoReceiveTransaction { get; set; }
}

public class CaHolderTransaction
{
    public List<IndexerTransaction> Data { get; set; } = new();
    public long TotalRecordCount { get; set; }
}

public class IndexerTransaction
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    public string TransactionId { get; set; }
    public string MethodName { get; set; }
    public int Platform { get; set; }
    public TokenInfo TokenInfo { get; set; }
    public NftInfo NftInfo { get; set; }
    public string Status { get; set; }
    public long Timestamp { get; set; }
    public TransferInfo TransferInfo { get; set; }
    public string FromAddress { get; set; }
    public List<IndexerTransactionFee> TransactionFees { get; set; }
    public bool IsManagerConsumer { get; set; }
    public string ToContractAddress { get; set; }
    public List<TokenTransferInfo> TokenTransferInfos { get; set; }
}

public class TokenTransferInfo
{
    public TokenInfo TokenInfo { get; set; }
    public NftInfo NftInfo { get; set; }
    public TransferInfo TransferInfo { get; set; }
}

public class TransferInfo
{
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    public long? Amount { get; set; }
    public string ToChainId { get; set; }
    public string FromChainId { get; set; }
    public string FromCAAddress { get; set; }
    public string TransferTransactionId { get; set; }
}

public class IndexerTransactionFee
{
    public string Symbol { get; set; }
    public long? Amount { get; set; }
}

public class IndexerSymbols
{
    public List<SymbolInfo> TokenInfo { get; set; }
}

public class SymbolInfo
{
    public int Decimals { get; set; }

    public string ChainId { get; set; }


}

public class CaHolderTransactionInfos
{
    public CaHolderTransaction CaHolderTransactionInfo { get; set; }
}