using System.Collections.Generic;

namespace CAServer.ContractEventHandler.Core.Application;

public class LoginGuardianChangeRecords
{
    public List<LoginGuardianChangeRecordDto> LoginGuardianChangeRecordInfo { get; set; }
}

public class ChangeRecordDto
{
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
    public string ChangeType { get; set; }
    public long BlockHeight { get; set; }
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