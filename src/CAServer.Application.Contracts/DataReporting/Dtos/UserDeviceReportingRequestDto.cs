using System.Collections.Generic;

namespace CAServer.DataReporting.Dtos;

public class UserDeviceReportingRequestDto
{
    public string DeviceId { get; set; }
    public string Token { get; set; }
    public long RefreshTime { get; set; }
    public NetworkType NetworkType { get; set; }
    public DeviceInfo DeviceInfo { get; set; }
    public List<string> LoginUserIds { get; set; }
}

public class DeviceInfo
{
    public DeviceType DeviceType { get; set; }
    public string DeviceBrand { get; set; }
    public string OperatingSystemVersion { get; set; }
}