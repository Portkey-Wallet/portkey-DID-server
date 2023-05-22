using System.Threading.Tasks;
using CAServer.AppleAuth;
using CAServer.AppleAuth.Dtos;
using CAServer.Google;
using CAServer.Google.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ThirdPartyAuth")]
[Route("api/app")]
public class ThirdPartyAuthController : CAServerController
{
    private readonly IAppleAuthAppService _appleAuthAppService;
    private readonly IGoogleAppService _googleAppService;
    private readonly AppleAuthOptions _appleAuthOptions;

    public ThirdPartyAuthController(IAppleAuthAppService appleAuthAppService,
        IOptions<AppleAuthOptions> appleAuthOptions,
        IGoogleAppService googleAppService)
    {
        _appleAuthAppService = appleAuthAppService;
        _googleAppService = googleAppService;
        _appleAuthOptions = appleAuthOptions.Value;
    }

    [HttpPost("appleAuth/receive")]
    public async Task<IActionResult> ReceiveAsync([FromForm] AppleAuthDto appleAuthDto)
    {
        await _appleAuthAppService.ReceiveAsync(appleAuthDto);
        return Redirect($"{_appleAuthOptions.RedirectUrl}?id_token={appleAuthDto.Id_token}");
    }

    [HttpPost("appleAuth/bingoReceive")]
    public async Task<IActionResult> BingoReceiveAsync([FromForm] AppleAuthDto appleAuthDto)
    {
        await _appleAuthAppService.ReceiveAsync(appleAuthDto);
        return Redirect($"{_appleAuthOptions.BingoRedirectUrl}?id_token={appleAuthDto.Id_token}");
    }

    [HttpPost("googleAuth/Receive")]
    public async Task<IActionResult> GoogleReceiveAsync([FromRoute] GoogleAuthDto googleAuthDto)
    {
        var result = await _googleAppService.ReceiveAsync(googleAuthDto);
        return Content(result);
    }
}