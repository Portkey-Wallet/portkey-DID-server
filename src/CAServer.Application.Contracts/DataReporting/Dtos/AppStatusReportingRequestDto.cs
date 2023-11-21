namespace CAServer.DataReporting.Dtos;

public class AppStatusReportingRequestDto
{
    public AppStatus Status { get; set; }
    public int UnreadCount { get; set; }
}