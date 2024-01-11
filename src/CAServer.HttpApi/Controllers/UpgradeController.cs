using System.Threading.Tasks;
using CAServer.Upgrade;
using CAServer.Upgrade.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Upgrade")]
[Route("api/app/upgrade")]
[Authorize]
public class UpgradeController : CAServerController
{
    private readonly IUpgradeAppService _upgradeAppService;

    public UpgradeController(IUpgradeAppService upgradeAppService)
    {
        _upgradeAppService = upgradeAppService;
    }

    [HttpGet("info")]
    public async Task<UpgradeResponseDto> GetUpgradeInfoAsync(UpgradeRequestDto input)
    {
        return await _upgradeAppService.GetUpgradeInfoAsync(input);
    }

    [HttpPost("close")]
    public async Task CloseAsync(UpgradeRequestDto input)
    {
        await _upgradeAppService.CloseAsync(input);
    }
}