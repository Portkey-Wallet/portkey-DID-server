using CAServer.Notify;

namespace CAServer.Grains.Grain.Notify;

[GenerateSerializer]
public class NotifyGrainResultDto : NotifyRulesBase
{
    [Id(0)]
    public Guid Id { get; set; }
}