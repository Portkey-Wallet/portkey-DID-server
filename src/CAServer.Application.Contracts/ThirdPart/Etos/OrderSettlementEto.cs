using CAServer.Commons.Etos;
using Volo.Abp.EventBus;

namespace CAServer.ThirdPart.Etos;

[EventName("OrderSettlementEto")]
public class OrderSettlementEto : BaseEto<OrderSettlementGrainDto>
{
    public OrderSettlementEto(OrderSettlementGrainDto data) : base(data)
    {
    }
}