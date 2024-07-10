using System.Threading.Tasks;
using CAServer.Facebook;
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
    private readonly ILogger<FacebookAuthController> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FacebookAuthController(IFacebookAuthAppService facebookAuthAppService,
        IOptionsSnapshot<FacebookOptions> facebookOptions, ILogger<FacebookAuthController> logger, IHttpContextAccessor httpContextAccessor)
    {
        _facebookAuthAppService = facebookAuthAppService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _facebookOptions = facebookOptions.Value;
    }

    [HttpGet("receive")]
    public async Task<IActionResult> ReceiveAsync(string code)
    {
        var querys = _httpContextAccessor.HttpContext.Request.QueryString;
        _logger.LogInformation("facebook request params:{querys}", querys.Value);
        _logger.LogDebug("=========================Get Code is {code}",code);
        var response = await _facebookAuthAppService.ReceiveAsync(code, ApplicationType.Receive);
        var result = response.Data;
        if (string.IsNullOrEmpty(response.Code))
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
        var response = await _facebookAuthAppService.ReceiveAsync(code, ApplicationType.UnifyReceive);
        var result = response.Data;
        if (string.IsNullOrEmpty(response.Code))
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