using Volo.Abp.EventBus;

namespace CAServer.Message.Etos;

[EventName("GetAlchemyTargetAddressEto")]
public class GetAlchemyTargetAddressEto : MessageBase<string>
{
    public string OrderId { get; set; }
    public string TargetClientId { get; set; }
}