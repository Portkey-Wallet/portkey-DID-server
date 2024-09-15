namespace CAServer;

public static class ContractAppServiceConstant
{
    public const long LongError = -1;
    public const long LongEmpty = 0;
    public const int IntError = -1;
    public const string MainChainId = "AELF";
}

public static class QueryLoginGuardianType
{
    public const string LoginGuardianRemoved = "LoginGuardianRemoved";
    public const string LoginGuardianUnbound = "LoginGuardianUnbound";
}

public static class QueryType
{
    public const string LoginGuardian = "LoginGuardianOn";
    public const string ManagerInfo = "ManagerInfoOn";
    public const string QueryRecord = "QueryRecord";
    public const string LoginGuardianChangeRecord = "LoginGuardianChangeRecord";
}

public static class LoggerMsg
{
    public const string IndexTimeoutError = "Index block height time out";
    public const string IndexBlockRecordInformation = "Block is not recorded, waiting...";
    public const string NodeBlockHeightWarning = "Current node block height should be large than the event";
}

public static class GrainId
{
    public const string SyncRecord = "SyncRecordGrain";
}

public static class LogEvent
{
    public const string CAHolderCreated = "CAHolderCreated";
    public const string NonCreateChainCAHolderCreated = "PreCrossChainSyncHolderInfoCreated";
    public const string ManagerInfoSocialRecovered = "ManagerInfoSocialRecovered";
    public const string CryptoBoxCreated = "CryptoBoxCreated";
    public const string CAHolderErrorOccured = "CAHolderErrorOccured";
    public const string GuardianAdded = "GuardianAdded";
    public const string GuardianRemoved = "GuardianRemoved";
    public const string GuardianUpdated = "GuardianUpdated";
    public const string LoginGuardianAdded = "LoginGuardianAdded";
    public const string LoginGuardianRemoved = "LoginGuardianRemoved";
    public const string TransferLimitChanged = "TransferLimitChanged";
    public const string ManagerInfoRemoved = "ManagerInfoRemoved";
    public const string ManagerApproved = "ManagerApproved";
}