using CAServer.ContractEventHandler;

namespace CAServer.Grains.State.ApplicationHandler;

[GenerateSerializer]
public class SyncRecordState
{
	[Id(0)]
    public List<SyncRecord> ValidatedRecords { get; set; }
	[Id(1)]
    public List<SyncRecord> ToBeValidatedRecords { get; set; }
}

[GenerateSerializer]
public class SyncRecord
{
	[Id(0)]
    public string CaHash { get; set; }
	[Id(1)]
    public string Manager { get; set; }
	[Id(2)]
    public string NotLoginGuardian { get; set; }
	[Id(3)]
    public string ChangeType { get; set; }
	[Id(4)]
    public long BlockHeight { get; set; }
	[Id(5)]
    public string BlockHash { get; set; }
	[Id(6)]
    public long ValidateHeight { get; set; }
	[Id(7)]
    public int RetryTimes { get; set; }
	[Id(8)]
    public TransactionInfo ValidateTransactionInfoDto { get; set; }
	[Id(9)]
    public DataSyncMonitor DataSyncMonitor { get; set; }
}

[GenerateSerializer]
public class TransactionInfo
{
	[Id(0)]
    public long BlockNumber { get; set; }
	[Id(1)]
    public string TransactionId { get; set; }
	[Id(2)]
    public byte[] Transaction { get; set; }
}
