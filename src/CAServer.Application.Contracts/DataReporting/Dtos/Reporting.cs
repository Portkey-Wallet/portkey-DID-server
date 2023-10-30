namespace CAServer.DataReporting.Dtos;

public class Reporting
{
    public string DeviceId { get; set; }
    public string Token { get; set; }
    public long RefreshTime { get; set; }
}

public class ReportingData
{
    public AppStatus Status { get; set; }
}