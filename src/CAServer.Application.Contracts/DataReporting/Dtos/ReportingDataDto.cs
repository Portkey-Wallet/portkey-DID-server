using System;

namespace CAServer.DataReporting.Dtos;

public class ReportingDataDto
{
    public Guid UserId { get; set; }
    public string DeviceId { get; set; }
    public AppStatus Status { get; set; }
}