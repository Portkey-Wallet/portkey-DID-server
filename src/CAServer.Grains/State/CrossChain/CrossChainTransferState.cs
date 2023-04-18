namespace CAServer.Grains.State.CrossChain;

public class CrossChainTransferState
{
    public Dictionary<string, CrossChainTransfer> CrossChainTransfers { get; set; } = new();
    public long LastedProcessedHeight { get; set; }
}

public class CrossChainTransfer
{
    public string Id { get; set; }
    public string FromChainId { get; set; }
    public string ToChainId { get; set; }
    public string TransferTransactionId { get; set; }
    public string TransferTransactionBlockHash { get; set; }
    public long TransferTransactionHeight { get; set; }
    public string ReceiveTransactionId { get; set; }
    public string ReceiveTransactionBlockHash { get; set; }
    public long ReceiveTransactionBlockHeight { get; set; }
    public long MainChainIndexHeight { get; set; }
    public CrossChainStatus Status { get; set; }
    public int RetryTimes { get; set; }
}

public enum CrossChainStatus
{
    Indexing,
    Receiving,
    Received,
    Confirmed
}