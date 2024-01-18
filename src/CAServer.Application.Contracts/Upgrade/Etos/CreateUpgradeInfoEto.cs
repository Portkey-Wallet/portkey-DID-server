using CAServer.Upgrade.Dtos;
using Volo.Abp.EventBus;

namespace CAServer.Upgrade.Etos;

[EventName(nameof(CreateUpgradeInfoEto))]
public class CreateUpgradeInfoEto : UpgradeBaseDto
{
}