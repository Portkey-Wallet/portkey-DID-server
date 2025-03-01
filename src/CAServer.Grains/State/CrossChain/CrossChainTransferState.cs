namespace CAServer.Grains.State.CrossChain;

[GenerateSerializer]
public class CrossChainTransferState
{
	[Id(0)]
    public Dictionary<string, CrossChainTransfer> CrossChainTransfers { get; set; } = new();
	[Id(1)]
    public long LastedProcessedHeight { get; set; }

	[Id(2)]
    public Dictionary<long, string> TransferTransactionDictionary { get; set; } = new();
}

[GenerateSerializer]
public class CrossChainTransfer
{
	[Id(0)]
    public string Id { get; set; }
	[Id(1)]
    public string FromChainId { get; set; }
	[Id(2)]
    public string ToChainId { get; set; }
	[Id(3)]
    public string TransferTransactionId { get; set; }
	[Id(4)]
    public string TransferTransactionBlockHash { get; set; }
	[Id(5)]
    public long TransferTransactionHeight { get; set; }
	[Id(6)]
    public string ReceiveTransactionId { get; set; }
	[Id(7)]
    public string ReceiveTransactionBlockHash { get; set; }
	[Id(8)]
    public long ReceiveTransactionBlockHeight { get; set; }
	[Id(9)]
    public long MainChainIndexHeight { get; set; }
	[Id(10)]
    public CrossChainStatus Status { get; set; }
	[Id(11)]
    public int RetryTimes { get; set; }
}

public enum CrossChainStatus
{
    Indexing,
    Receiving,
    Received,
    Confirmed
}
