using CAServer.Notify;

namespace CAServer.Grains.Grain.Notify;

public class NotifyGrainResultDto : NotifyRulesBase
{
    public Guid Id { get; set; }
}