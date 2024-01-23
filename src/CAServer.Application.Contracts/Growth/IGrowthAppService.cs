using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Growth.Dtos;

namespace CAServer.Growth;

public interface IGrowthAppService
{
    Task<GrowthRedDotDto> GetRedDotAsync();
    Task SetRedDotAsync();
    Task<ShortLinkDto> GetShortLinkAsync(string projectCode);
    Task CreateGrowthInfoAsync(string caHash, ReferralInfo referralInfo);
    Task<string> GetRedirectUrlAsync(string shortLinkCode);
    Task<ReferralResponseDto> GetReferralInfoAsync(ReferralRequestDto input);
}