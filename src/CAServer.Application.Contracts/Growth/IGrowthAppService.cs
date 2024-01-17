using System.Threading.Tasks;
using CAServer.Growth.Dtos;

namespace CAServer.Growth;

public interface IGrowthAppService
{
    Task<GrowthRedDotDto> GetRedDotAsync();
    Task SetRedDotAsync();
    Task<ShortLinkDto> GetShortLinkAsync(string projectCode);
    Task CreateGrowthInfoAsync(string referralCode);
    Task<string> GetRedirectUrlAsync(string shortLinkCode);
}