using Volo.Abp.EventBus;

namespace CAServer.Message.Etos;

[EventName("AlchemyTargetAddressEto")]
public class AlchemyTargetAddressEto : MessageBase<string>
{
    public string OrderId { get; set; }
    public string TargetClientId { get; set; }
}