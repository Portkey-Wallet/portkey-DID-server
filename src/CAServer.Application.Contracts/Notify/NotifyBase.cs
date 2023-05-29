using System;

namespace CAServer.Notify;

public class NotifyBase
{
    public string Title { get; set; }
    public string Content { get; set; }
    public string AppId { get; set; }
    public string TargetVersion { get; set; }
    public string DownloadUrl { get; set; }
    public DateTime ReleaseTime { get; set; }
    public bool IsForceUpdate { get; set; }
    public StyleType StyleType { get; set; }
}