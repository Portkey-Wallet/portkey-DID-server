using Volo.Abp.EventBus;

namespace CAServer.Tab.Etos;

[EventName("TabCompleteEto")]
public class TabCompleteEto
{
    public string ClientId { get; set; }
    public string MethodName { get; set; }
    public string Data { get; set; }
    public string TargetClientId { get; set; }
}