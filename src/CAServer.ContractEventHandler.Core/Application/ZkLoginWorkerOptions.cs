namespace CAServer.ContractEventHandler.Core.Application;

public class ZkLoginWorkerOptions
{
    public int PeriodSeconds { get; set; } = 86400;

    public int LoopSize { get; set; } = 30;

    public int TotalHolders { get; set; }

    public int ExpirationSeconds { get; set; } = 3600;
}