using System.Collections.Generic;

namespace CAServer.CAActivity.Provider;

public class IndexerTransactions
{
    public List<IndexerTransaction> CaHolderTransaction { get; set; }
}

public class IndexerTransaction
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public string TransactionId { get; set; }
    public string MethodName { get; set; }
    public TokenInfo TokenInfo { get; set; }
    public NftInfo NftInfo { get; set; }
    public string Status { get; set; }
    public long Timestamp { get; set; }
    public TransferInfo TransferInfo { get; set; }
    public string FromAddress { get; set; }
    public List<IndexerTransactionFee> TransactionFees { get; set; }
}

public class TokenInfo
{
    public string Symbol { get; set; }
    public int? Decimals { get; set; }
}

public class NftInfo
{
    public string Url { get; set; }
    public string Alias { get; set; }
    public long? NftId { get; set; }
}

public class TransferInfo
{
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    public long? Amount { get; set; }
    public string ToChainId { get; set; }
    public string FromChainId { get; set; }
}

public class IndexerTransactionFee
{
    public string Symbol { get; set; }
    public long? Amount { get; set; }
}