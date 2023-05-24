using System.ComponentModel.DataAnnotations;

namespace CAServer.Notify.Dtos;

public class PullNotifyDto
{
    [Required] public string DeviceId { get; set; }
    [Required] public DeviceType DeviceType { get; set; }
    [Required] public string AppVersion { get; set; }
    public string DeviceBrand { get; set; }
    public string OperatingSystemVersion { get; set; }
    [Required] public string AppId { get; set; }
}