using CAServer.Monitor.Commons;

namespace CAServer.Monitor;

public class InterIndicator
{
    public InterIndicator()
    {
        StartTime = MonitorTimeHelper.GetTimeStampInMilliseconds();
    }

    public InterIndicator(MonitorTag tag, string target)
    {
        Tag = tag;
        Target = target;
        StartTime = MonitorTimeHelper.GetTimeStampInMilliseconds();
    }

    public MonitorTag Tag { get; set; }
    public string Target { get; set; }
    public long StartTime { get; set; }
    public int Value { get; set; }
}