using CAServer.Notify;

namespace CAServer.Grains.State.Notify;

[GenerateSerializer]
public class NotifyRulesState : NotifyRulesBase
{
	[Id(0)]
    public Guid Id { get; set; }
}
