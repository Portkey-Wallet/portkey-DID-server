using CAServer.DataReporting;

namespace CAServer.Models;

public class ReportingData
{
    public AppStatus Status { get; set; }
}

public class Reporting
{
    public string DeviceId { get; set; }
    public string Token { get; set; }
    public long RefreshTime { get; set; }
}