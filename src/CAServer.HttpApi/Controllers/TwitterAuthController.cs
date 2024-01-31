using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.TwitterAuth;
using CAServer.TwitterAuth.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("TwitterAuth")]
[Route("api/app/twitterAuth")]
public class TwitterAuthController : CAServerController
{
    private readonly ITwitterAuthAppService _twitterAuthAppService;
    private readonly TwitterAuthOptions _options;

    public TwitterAuthController(ITwitterAuthAppService twitterAuthAppService,
        IOptionsSnapshot<TwitterAuthOptions> options)
    {
        _twitterAuthAppService = twitterAuthAppService;
        _options = options.Value;
    }

    [HttpGet("receive")]
    public async Task<IActionResult> ReceiveAsync([FromQuery] TwitterAuthDto twitterAuthDto)
    {
        var accessToken = await _twitterAuthAppService.ReceiveAsync(twitterAuthDto);
        var redirectUrl = GetRedirectUrl(_options.UnifyRedirectUrl, accessToken);
        return Redirect(redirectUrl);
    }

    [HttpPost("unifyReceive")]
    public async Task<IActionResult> UnifyReceiveAsync([FromQuery] TwitterAuthDto twitterAuthDto)
    {
        var accessToken = await _twitterAuthAppService.ReceiveAsync(twitterAuthDto);
        var redirectUrl = GetRedirectUrl(_options.UnifyRedirectUrl, accessToken);
        return Redirect(redirectUrl);
    }

    private string GetRedirectUrl(string redirectUrl, TwitterRedirectInfoDto redirectInfo)
    {
        if (redirectUrl.Contains("?"))
        {
            return
                $"{redirectUrl}&token={redirectInfo.AccessToken}&id={redirectInfo.UserInfo.Id}&name={redirectInfo.UserInfo.Name}&username={redirectInfo.UserInfo.UserName}";
        }

        return $"{redirectUrl}?token={redirectInfo.AccessToken}&id={redirectInfo.UserInfo.Id}&name={redirectInfo.UserInfo.Name}&username={redirectInfo.UserInfo.UserName}";
    }
}