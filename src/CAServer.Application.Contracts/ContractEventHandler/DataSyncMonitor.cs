using System;
using System.Collections.Generic;
using CAServer.Monitor;
using Orleans;

namespace CAServer.ContractEventHandler;

[GenerateSerializer]
public class DataSyncMonitor
{
    [Id(0)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    [Id(1)]
    public int TotalTime { get; set; }
    [Id(2)]
    public DateTime StartTime { get; set; }
    [Id(3)]
    public string CaHash { get; set; }
    [Id(4)]
    public string ChangeType { get; set; }
    [Id(5)]
    public List<MonitorNode> MonitorNodes { get; set; }
}