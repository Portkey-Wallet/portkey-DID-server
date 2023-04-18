namespace CAServer.ContractEventHandler.Core.Application;

public static class ContractAppServiceConstant
{
    public const long LongError = -1;
    public const long LongEmpty = 0;
    public const int IntError = -1;
}

public static class QueryLoginGuardianAccountType
{
    public const string LoginGuardianAccountAdded = "LoginGuardianAccountAdded";
    public const string LoginGuardianAccountUnbound = "LoginGuardianAccountUnbound";
}

public static class QueryType
{
    public const string LoginGuardianAccount = "LoginGuardianAccountOn";
    public const string Manager = "ManagerOn";
}