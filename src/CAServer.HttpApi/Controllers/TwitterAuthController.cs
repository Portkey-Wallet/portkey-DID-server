using System;
using System.Threading.Tasks;
using Asp.Versioning;
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

    [HttpGet("userInfo")]
    public async Task<TwitterUserAuthInfoDto> GetUserAuthAsync(TwitterAuthDto twitterAuthDto)
    {
        var authResult = await _twitterAuthAppService.ReceiveAsync(twitterAuthDto);
        if (!authResult.Code.IsNullOrEmpty())
        {
            throw new UserFriendlyException(authResult.Message, authResult.Code);
        }

        return authResult.Data;
    }

    [HttpGet("receive")]
    public async Task<IActionResult> ReceiveAsync([FromQuery] TwitterAuthDto twitterAuthDto)
    {
        var authResult = await _twitterAuthAppService.ReceiveAsync(twitterAuthDto);
        var redirectUrl = GetRedirectUrl(_options.RedirectUrl, authResult);
        return Redirect(redirectUrl);
    }

    [HttpGet("unifyReceive")]
    public async Task<IActionResult> UnifyReceiveAsync([FromQuery] TwitterAuthDto twitterAuthDto)
    {
        var authResult = await _twitterAuthAppService.ReceiveAsync(twitterAuthDto);
        var redirectUrl = GetRedirectUrl(_options.UnifyRedirectUrl, authResult);
        return Redirect(redirectUrl);
    }

    private string GetRedirectUrl(string redirectUrl, TwitterAuthResultDto twitterAuthResultDto)
    {
        if (twitterAuthResultDto.Code.IsNullOrEmpty())
        {
            return GetRedirectUrl(redirectUrl, twitterAuthResultDto.Data);
        }

        var requestParams = $"code={twitterAuthResultDto.Code}&message={twitterAuthResultDto.Message}";
        if (redirectUrl.Contains(CommonConstant.UrlSegmentation))
        {
            return
                $"{redirectUrl}&{requestParams}";
        }

        return
            $"{redirectUrl}?{requestParams}";
    }

    private string GetRedirectUrl(string redirectUrl, TwitterUserAuthInfoDto userAuthInfo)
    {
        var requestParams =
            $"token={userAuthInfo.AccessToken}&id={userAuthInfo.UserInfo.Id}&name={userAuthInfo.UserInfo.Name}&username={userAuthInfo.UserInfo.UserName}&type={userAuthInfo.AuthType}";
        if (redirectUrl.Contains(CommonConstant.UrlSegmentation))
        {
            return
                $"{redirectUrl}&{requestParams}";
        }

        return
            $"{redirectUrl}?{requestParams}";
    }
}