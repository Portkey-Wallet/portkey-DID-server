using System;

namespace CAServer.DataReporting.Dtos;

public class AppStatusReporting
{
    public Guid UserId { get; set; }
    public string DeviceId { get; set; }
    public AppStatus Status { get; set; }
    public NetworkType NetworkType { get; set; }
}