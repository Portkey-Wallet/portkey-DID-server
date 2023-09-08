using CAServer.Commons.Etos;
using Volo.Abp.EventBus;

namespace CAServer.ThirdPart.Etos;

[EventName("NFTOrderEto")]
public class NftOrderEto : BaseEto<NftOrderGrainDto>
{
    
    public NftOrderEto(NftOrderGrainDto data) : base(data)
    {
    }
    
}