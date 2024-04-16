using System.Collections.Generic;

namespace CAServer.DataReporting.Dtos;

public class AppStatusReportingRequestDto
{
    public AppStatus Status { get; set; }
    public int UnreadCount { get; set; }
    public List<string> LoginUserIds { get; set; }
}