using System;

namespace CAServer.Notify.Dtos;

public class NotifyResultDto : NotifyBase
{
    public Guid Id { get; set; }
    
    public string[] AppVersions { get; set; }
    public string[] DeviceTypes { get; set; }
    public string[] DeviceBrands { get; set; }
    public string[] OperatingSystemVersions { get; set; }
    public NotifySendType[] SendTypes { get; set; }
    public bool IsApproved { get; set; }
}