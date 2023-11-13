namespace CAServer.DataReporting.Dtos;

public class AppStatusReportingRequestDto
{
    public NetworkType NetworkType { get; set; }
    public AppStatus Status { get; set; }
}