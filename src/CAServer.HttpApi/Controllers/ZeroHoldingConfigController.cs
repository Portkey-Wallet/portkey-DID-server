using System.Threading.Tasks;
using CAServer.UserAssets;
using CAServer.ZeroHoldings;
using CAServer.ZeroHoldings.constant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using CAServer.ZeroHoldings.Dtos;
namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ZeroHoldings")]
[Route("api/app/assets/zeroHoldings")]
[Authorize]
public class ZeroHoldingConfigController
{
    private readonly IZeroHoldingsConfigAppService _zeroHoldingsConfigAppService;

    public ZeroHoldingConfigController(IZeroHoldingsConfigAppService zeroHoldingsConfigAppService)
    {
        _zeroHoldingsConfigAppService = zeroHoldingsConfigAppService;
    }

    [HttpPost("close")]
    public async Task<bool> SetToClose()
    {
        await _zeroHoldingsConfigAppService.SetStatus(new ZeroHoldingsConfigDto{Status = ZeroHoldingsConfigConstant.CloseStatus});
        return true;
    }
    
    [HttpPost("open")]
    public async Task<bool> SetToOpen(GetTokenRequestDto requestDto)
    {
        await _zeroHoldingsConfigAppService.SetStatus(new ZeroHoldingsConfigDto{Status = ZeroHoldingsConfigConstant.OpenStatus});
        return true;
    }
    
    [HttpPost("status")]
    public async Task<ZeroHoldingsConfigDto> GetStatus(GetTokenRequestDto requestDto)
    {
        ZeroHoldingsConfigDto status = await _zeroHoldingsConfigAppService.GetStatus();
        return status;
    }
}