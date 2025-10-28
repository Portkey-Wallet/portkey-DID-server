using CAServer.Notify;

namespace CAServer.Grains.State.Notify;

[GenerateSerializer]
public class NotifyState : NotifyBase
{
	[Id(0)]
    public Guid Id { get; set; }
	[Id(1)]
    public Guid RulesId { get; set; }
	[Id(2)]
    public int NotifyId { get; set; }
	[Id(3)]
    public bool IsDeleted { get; set; }
}
