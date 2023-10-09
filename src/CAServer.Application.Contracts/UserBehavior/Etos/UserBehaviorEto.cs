using Volo.Abp.EventBus;

namespace CAServer.UserBehavior.Etos;

[EventName("UserBehaviorEto")]
public class UserBehaviorEto
{
    public string DappName { get; set; }
    public string CaAddress { get; set; }
    public string Action { get; set; }
    public string Referrer { get; set; }
    public string UserAgent { get; set; }
}