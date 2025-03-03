using CAServer.Notify;

namespace CAServer.Grains.Grain.Notify;

[GenerateSerializer]
public class NotifyGrainDto : NotifyRulesBase
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public int NotifyId { get; set; }
    [Id(2)]
    public string Title { get; set; }
    [Id(3)]
    public string Content { get; set; }
    [Id(4)]
    public string TargetVersion { get; set; }
    [Id(5)]
    public string DownloadUrl { get; set; }
    [Id(6)]
    public StyleType StyleType { get; set; }
    [Id(7)]
    public DateTime ReleaseTime { get; set; }
    [Id(8)]
    public bool IsForceUpdate { get; set; }
}