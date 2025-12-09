using CAServer.Commons.Etos;
using Volo.Abp.EventBus;

namespace CAServer.ThirdPart.Etos;

[EventName("PendingTreasuryOrderEto")]
public class PendingTreasuryOrderEto: BaseEto<PendingTreasuryOrderDto>
{
    
    public PendingTreasuryOrderEto(PendingTreasuryOrderDto data) : base(data)
    {
    }
    
}