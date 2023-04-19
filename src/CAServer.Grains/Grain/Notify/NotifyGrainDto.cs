
using CAServer.Notify;

namespace CAServer.Grains.Grain.Notify;

public class NotifyGrainDto : NotifyRulesBase
{
    public Guid Id { get; set; }

    public string Title { get; set; }
    public string Content { get; set; }
    public string TargetVersion { get; set; }
    public string DownloadUrl { get; set; }
    public DateTime ReleaseTime { get; set; }
    public bool IsForceUpdate { get; set; }
}