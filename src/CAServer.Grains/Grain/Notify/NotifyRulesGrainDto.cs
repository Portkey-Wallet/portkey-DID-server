using CAServer.Notify;

namespace CAServer.Grains.Grain.Notify;

[GenerateSerializer]
public class NotifyRulesGrainDto : NotifyRulesBase
{
    [Id(0)]
    public Guid Id { get; set; }
}