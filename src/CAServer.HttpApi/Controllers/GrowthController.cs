using System.Threading.Tasks;
using CAServer.Growth;
using CAServer.Growth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Growth")]
[Route("api/app/growth")]
public class GrowthController : CAServerController
{
    private readonly IGrowthAppService _growthAppService;
    private readonly IGrowthStatisticAppService _statisticAppService;

    public GrowthController(IGrowthAppService growthAppService, IGrowthStatisticAppService statisticAppService)
    {
        _growthAppService = growthAppService;
        _statisticAppService = statisticAppService;
    }

    [HttpGet("redDot"), Authorize]
    public async Task<GrowthRedDotDto> GetRedDotAsync()
    {
        return await _growthAppService.GetRedDotAsync();
    }

    [HttpPost("redDot"), Authorize]
    public async Task SetRedDotAsync()
    {
        await _growthAppService.SetRedDotAsync();
    }

    [HttpGet("shortLink"), Authorize]
    public async Task<ShortLinkDto> GetShortLinkAsync(string projectCode)
    {
        return await _growthAppService.GetShortLinkAsync(projectCode);
    }

    [HttpGet("referralInfo"), Authorize(Roles = "admin")]
    public async Task<ReferralResponseDto> GetReferralInfoAsync(ReferralRequestDto input)
    {
        return await _statisticAppService.GetReferralInfoAsync(input);
    }
}