namespace CAServer.Grains.State.ApplicationHandler;

public class SyncRecordState
{
    public List<SyncRecord> ValidatedRecords { get; set; }
    public List<SyncRecord> ToBeValidatedRecords { get; set; }
}

public class SyncRecord
{
    public string CaHash { get; set; }
    public string Manager { get; set; }
    public string NotLoginGuardian { get; set; }
    public string ChangeType { get; set; }
    public long BlockHeight { get; set; }
    public string BlockHash { get; set; }
    public long ValidateHeight { get; set; }
    public int RetryTimes { get; set; }
    public TransactionInfo ValidateTransactionInfoDto { get; set; }

    public RecordStatus RecordStatus { get; set; } = RecordStatus.NONE;
}

public enum RecordStatus
{
    NONE,
    SYNCED,
    NOT_MINED,
    MINED
}

public class TransactionInfo
{
    public long BlockNumber { get; set; }
    public string TransactionId { get; set; }
    public byte[] Transaction { get; set; }
}