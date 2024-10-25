using System.Collections.Generic;

namespace CAServer;

public class LoginGuardianChangeRecords
{
    public List<LoginGuardianChangeRecordDto> LoginGuardianChangeRecordInfo { get; set; }
}

public class ChangeRecordDto
{
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
    public string ChangeType { get; set; }
    public string Manager { get; set; }
    public long BlockHeight { get; set; }
    public string BlockHash { get; set; }
}

public class LoginGuardianChangeRecordDto : ChangeRecordDto
{
    public string Id { get; set; }
    public LoginGuardian LoginGuardian { get; set; }
}

public class LoginGuardian
{
    public string IdentifierHash { get; set; }
}

public class CAHolderManagerChangeRecords
{
    public List<CAHolderManagerChangeRecordDto> CaHolderManagerChangeRecordInfo { get; set; }
}

public class CAHolderManagerChangeRecordDto : ChangeRecordDto
{
    public string Address { get; set; }
}

public class ConfirmedBlockHeightRecord
{
    public SyncState SyncState { get; set; }
}

public class SyncState
{
    public long ConfirmedBlockHeight { get; set; }
}

public class SyncStateDto
{
    public  IndexerVersionInfo CurrentVersion { get; set; }
}

public class IndexerVersionInfo
{
    public List<SyncStateItem> Items { get; set; } = new();
}

public class SyncStateItem
{
    public string ChainId { get; set; }
    public string LongestChainBlockHash { get; set; }
    public long LongestChainHeight { get; set; }
    public string BestChainBlockHash { get; set; }
    public long BestChainHeight { get; set; }
    public string LastIrreversibleBlockHash { get; set; }
    public long LastIrreversibleBlockHeight { get; set; }
}

public class QueryEventDto : ChangeRecordDto
{
    public string NotLoginGuardian { get; set; }
}

public enum BlockFilterType
{
    BLOCK,
    TRANSACTION,
    LOG_EVENT
}

public class GuardianChangeRecords
{
    public List<GuardianChangeRecord> GuardianChangeRecordInfo { get; set; }
}

public class GuardianChangeRecord : ChangeRecordDto
{
}