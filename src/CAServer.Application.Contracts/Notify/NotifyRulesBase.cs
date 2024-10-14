using Orleans;

namespace CAServer.Notify;

[GenerateSerializer]
public class NotifyRulesBase
{
    [Id(0)]
    public int NotifyId { get; set; }
    [Id(1)]
    public string AppId { get; set; }
    [Id(2)]
    public string[] AppVersions { get; set; }
    [Id(3)]
    public string[] DeviceTypes { get; set; }
    [Id(4)]
    public string[] DeviceBrands { get; set; }
    [Id(5)]
    public string[] OperatingSystemVersions { get; set; }
    [Id(6)]
    public string[] Countries { get; set; }
    [Id(7)]
    public NotifySendType[] SendTypes { get; set; }
    [Id(8)]
    public bool IsApproved { get; set; }
}