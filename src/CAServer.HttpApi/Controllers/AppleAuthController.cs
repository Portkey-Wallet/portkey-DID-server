using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.AppleAuth;
using CAServer.AppleAuth.Dtos;
using CAServer.Commons;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("AppleAuth")]
[Route("api/app/appleAuth/")]
public class AppleAuthController : CAServerController
{
    private readonly IAppleAuthAppService _appleAuthAppService;
    private readonly AppleAuthOptions _appleAuthOptions;

    public AppleAuthController(IAppleAuthAppService appleAuthAppService,
        IOptions<AppleAuthOptions> appleAuthOptions)
    {
        _appleAuthAppService = appleAuthAppService;
        _appleAuthOptions = appleAuthOptions.Value;
    }

    [HttpPost("receive")]
    public async Task<IActionResult> ReceiveAsync([FromForm] AppleAuthDto appleAuthDto)
    {
        await _appleAuthAppService.ReceiveAsync(appleAuthDto);
        var redirectUrl = GetRedirectUrl(_appleAuthOptions.RedirectUrl, appleAuthDto.Id_token);
        return Redirect(redirectUrl);
    }

    [HttpPost("bingoReceive")]
    public async Task<IActionResult> BingoReceiveAsync([FromForm] AppleAuthDto appleAuthDto)
    {
        await _appleAuthAppService.ReceiveAsync(appleAuthDto);
        var redirectUrl = GetRedirectUrl(_appleAuthOptions.BingoRedirectUrl, appleAuthDto.Id_token);
        return Redirect(redirectUrl);
    }

    [HttpPost("unifyReceive")]
    public async Task<IActionResult> UnifyReceiveAsync([FromForm] AppleAuthDto appleAuthDto)
    {
        await _appleAuthAppService.ReceiveAsync(appleAuthDto);
        var redirectUrl = GetRedirectUrl(_appleAuthOptions.UnifyRedirectUrl, appleAuthDto.Id_token);
        return Redirect(redirectUrl);
    }

    private string GetRedirectUrl(string redirectUrl, string token)
    {
        if (redirectUrl.Contains(CommonConstant.UrlSegmentation))
        {
            return $"{redirectUrl}&id_token={token}";
        }

        return $"{redirectUrl}?id_token={token}";
    }
}