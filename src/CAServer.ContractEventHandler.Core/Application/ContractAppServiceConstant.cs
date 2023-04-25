namespace CAServer.ContractEventHandler.Core.Application;

public static class ContractAppServiceConstant
{
    public const long LongError = -1;
    public const long LongEmpty = 0;
    public const int IntError = -1;
    public const string MainChainId = "AELF";
}

public static class QueryLoginGuardianType
{
    public const string LoginGuardianAdded = "LoginGuardianAdded";
    public const string LoginGuardianUnbound = "LoginGuardianUnbound";
}

public static class QueryType
{
    public const string LoginGuardian = "LoginGuardianOn";
    public const string ManagerInfo = "ManagerInfoOn";
    public const string QueryRecord = "QueryRecord";
}

public static class LoggerMsg
{
    public const string IndexTimeoutError = "Index block height time out";
    public const string IndexBlockRecordInformation = "Block is not recorded, waiting...";
    public const string NodeBlockHeightWarning = "Current node block height should be large than the event";
}