using System;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Notify.Dtos;

public class NotifyBaseDto
{
    [Required] public string Title { get; set; }
    [Required] public string Content { get; set; }
    [Required] public string AppId { get; set; }
    [Required] public string TargetVersion { get; set; }
    public string[] AppVersions { get; set; }
    [Required] public string DownloadUrl { get; set; }
    [Required] public DateTime ReleaseTime { get; set; }
    [Required] public DeviceType[] DeviceTypes { get; set; }
    public string[] DeviceBrands { get; set; }
    public string[] OperatingSystemVersions { get; set; }
    public NotifySendType[] SendTypes { get; set; }
    [Required] public bool IsForceUpdate { get; set; }
    public int NotifyId { get; set; }
    public bool IsApproved { get; set; }
}