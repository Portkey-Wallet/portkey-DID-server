using System;
using System.Collections.Generic;
using CAServer.Monitor;

namespace CAServer.ContractEventHandler;

public class DataSyncMonitor
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int TotalTime { get; set; }
    public DateTime StartTime { get; set; }
    public string CaHash { get; set; }
    public string ChangeType { get; set; }
    public List<MonitorNode> MonitorNodes { get; set; }
}