using System.Threading.Tasks;
using CAServer.Upgrade.Dtos;

namespace CAServer.Upgrade;

public interface IUpgradeAppService
{
    Task<UpgradeResponseDto> GetUpgradeInfoAsync(UpgradeRequestDto input);
    Task CloseAsync(UpgradeRequestDto input);
}