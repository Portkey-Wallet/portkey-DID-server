using System;

namespace CAServer.DataReporting.Dtos;

public class AppStatusReportingDto
{
    public Guid UserId { get; set; }
    public string DeviceId { get; set; }
    public AppStatus Status { get; set; }
    public NetworkType NetworkType { get; set; }
}