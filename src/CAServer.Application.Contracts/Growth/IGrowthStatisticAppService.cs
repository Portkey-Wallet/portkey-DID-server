using System.Threading.Tasks;
using CAServer.Growth.Dtos;

namespace CAServer.Growth;

public interface IGrowthStatisticAppService
{
    Task<ReferralResponseDto> GetReferralInfoAsync(ReferralRequestDto input);
}