using CAServer.Commons.Etos;
using Volo.Abp.EventBus;

namespace CAServer.ThirdPart.Etos;


[EventName("TreasuryOrderEto")]
public class TreasuryOrderEto : BaseEto<TreasuryOrderDto>
{
    
    public TreasuryOrderEto(TreasuryOrderDto data) : base(data)
    {
    }
    
}