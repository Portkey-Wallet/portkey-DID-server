using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.Facebook;
using CAServer.Facebook.Dtos;
using CAServer.Verifier;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("FacebookAuth")]
[Route("api/app/facebookAuth/")]
public class FacebookAuthController : CAServerController
{
    private readonly IFacebookAuthAppService _facebookAuthAppService;
    private readonly FacebookOptions _facebookOptions;


    public FacebookAuthController(IFacebookAuthAppService facebookAuthAppService,
        IOptionsSnapshot<FacebookOptions> facebookOptions)
    {
        _facebookAuthAppService = facebookAuthAppService;

        _facebookOptions = facebookOptions.Value;
    }

    [HttpGet("receive")]
    public async Task<IActionResult> ReceiveAsync(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            var redirectUrl = _facebookOptions.FacebookAuthUrl + "/portkey-auth-callback?type=Facebook";
            return Redirect(redirectUrl);
        }

        var response = await _facebookAuthAppService.ReceiveAsync(code, ApplicationType.Receive);
        var result = response.Data;
        if (result != null)
        {
            var redirectUrl = _facebookOptions.FacebookAuthUrl + "/portkey-auth-callback?userId="
                                                               + result.UserId + "&token=" + result.AccessToken +
                                                               "&expiresTime=" + result.ExpiresTime + "&type=Facebook";
            return Redirect(redirectUrl);
        }

        var errorRedirectUrl = _facebookOptions.FacebookAuthUrl + "/portkey-auth-callback?code="
                                                                + response.Code + "&message=" + response.Message +
                                                                "&type=Facebook";


        return Redirect(errorRedirectUrl);
    }

    [HttpGet("unifyReceive")]
    public async Task<IActionResult> UnifyReceiveAsync(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            var redirectUrl = _facebookOptions.FacebookAuthUrl + "/portkey-auth-callback?type=Facebook";
            return Redirect(redirectUrl);
        }
        var response = await _facebookAuthAppService.ReceiveAsync(code, ApplicationType.UnifyReceive);
        var result = response.Data;
        if (result != null)
        {
            var redirectUrl = _facebookOptions.FacebookAuthUrl + "/auth-callback?userId=" + result.UserId + "&token=" +
                              result.AccessToken + "&expiresTime=" + result.ExpiresTime + "&type=Facebook";
            return Redirect(redirectUrl);
        }

        var errorRedirectUrl = _facebookOptions.FacebookAuthUrl + "/auth-callback?code=" + response.Code + "&message=" +
                               response.Message + "&type=Facebook";
        return Redirect(errorRedirectUrl);
    }
}