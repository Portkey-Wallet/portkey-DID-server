using CAServer.Growth.Dtos;
using Volo.Abp.EventBus;

namespace CAServer.Growth.Etos;

[EventName(nameof(CreateGrowthEto))]
public class CreateGrowthEto : GrowthBase
{
}