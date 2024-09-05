namespace CAServer.ContractEventHandler.Core.Application;

public class NotifyWorkerOptions
{
    public int PeriodSeconds { get; set; } = 60;
    
    public int ExpirationSeconds { get; set; } = 3600;
    
    public int MaxResultCount { get; set; } = 100;

    public int CacheMinutes { get; set; } = 2;
}