namespace CAServer.Monitor;

[GenerateSerializer]
public class MonitorNode
{
    [Id(0)]
    public string Name { get; set; }
    [Id(1)]
    public long StartTime { get; set; }
    [Id(2)]
    public int Duration { get; set; }
}