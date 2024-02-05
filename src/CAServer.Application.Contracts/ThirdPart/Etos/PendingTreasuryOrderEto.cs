using CAServer.Commons.Etos;
using Volo.Abp.EventBus;

namespace CAServer.ThirdPart.Etos;

[EventName("TransactionEto")]
public class PendingTreasuryOrderEto: BaseEto<PendingTreasuryOrderDto>
{
    
    public PendingTreasuryOrderEto(PendingTreasuryOrderDto data) : base(data)
    {
    }
    
}