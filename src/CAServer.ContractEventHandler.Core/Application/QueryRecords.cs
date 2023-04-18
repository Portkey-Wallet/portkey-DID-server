using System.Collections.Generic;

namespace CAServer.ContractEventHandler.Core.Application;

public class LoginGuardianAccountChangeRecords
{
    public List<LoginGuardianAccountChangeRecordDto> LoginGuardianAccountChangeRecordInfo { get; set; }
}

public class ChangeRecordDto
{
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
    public string ChangeType { get; set; }
    public long BlockHeight { get; set; }
}

public class LoginGuardianAccountChangeRecordDto : ChangeRecordDto
{
    public string Id { get; set; }
    public LoginGuardianAccount LoginGuardianAccount { get; set; }
}

public class LoginGuardianAccount
{
    public string Value { get; set; }
}

public class CAHolderManagerChangeRecords
{
    public List<CAHolderManagerChangeRecordDto> CaHolderManagerChangeRecordInfo { get; set; }
}

public class CAHolderManagerChangeRecordDto : ChangeRecordDto
{
    public string Manager { get; set; }
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
    public string Value { get; set; }
}

public enum BlockFilterType
{
    BLOCK,
    TRANSACTION,
    LOG_EVENT
}