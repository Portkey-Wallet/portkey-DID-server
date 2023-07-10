using CAServer.Notify;

namespace CAServer.Grains.State.Notify;

public class NotifyState : NotifyBase
{
    public Guid Id { get; set; }
    public Guid RulesId { get; set; }
    public int NotifyId { get; set; }
    public bool IsDeleted { get; set; }
}