using Volo.Abp.EventBus;

namespace CAServer.UserBehavior.Etos;

[EventName("UserBehaviorEto")]
public class UserBehaviorEto
{
    public string DappName { get; set; }
    public string CaAddress { get; set; }
    public string CaHash { get; set; }
    public string UserId { get; set; }
    public UserBehaviorAction Action { get; set; }
    public string Referer { get; set; }
    public string UserAgent { get; set; }
    public string Origin { get; set; }
    public bool Result { get; set; }
    public string ChainId { get; set; }
    public string SessionId { get; set; }
}