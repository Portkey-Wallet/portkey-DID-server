namespace CAServer.Notify;

public class NotifyRulesBase
{
    public int NotifyId { get; set; }
    public string AppId { get; set; }
    public string[] AppVersions { get; set; }
    public string[] DeviceTypes { get; set; }
    public string[] DeviceBrands { get; set; }
    public string[] OperatingSystemVersions { get; set; }
    public string[] Countries { get; set; }
    public NotifySendType[] SendTypes { get; set; }
    public bool IsApproved { get; set; }
}