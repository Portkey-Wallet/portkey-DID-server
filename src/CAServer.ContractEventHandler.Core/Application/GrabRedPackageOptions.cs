namespace CAServer.ContractEventHandler.Core.Application;

public class GrabRedPackageOptions
{
    public int Interval { get; set; } = 10;
    public int FirstRecurringCount { get; set; } = 5;
    public int SecondRecurringCount { get; set; } = 15;
    public int RecurringInfoExpireDays { get; set; } = 2;
    public int PayCacheExpireTime { get; set; } = 3;
}