using CAServer.Grains.Grain.ApplicationHandler;

namespace CAServer.Grains.State.ApplicationHandler;

public class SyncRecordState
{
    public List<SyncRecord> Records { get; set; }
    public List<SyncRecord> FailedRecords { get; set; }
}

public class SyncRecord
{
    public string CaHash { get; set; }
    public string NotLoginGuardian { get; set; }
    public string ChangeType { get; set; }
    public long BlockHeight { get; set; }
    public long ValidateHeight { get; set; }
    public int RetryTimes { get; set; }
    public TransactionInfoDto ValidateTransactionInfoDto { get; set; }
}