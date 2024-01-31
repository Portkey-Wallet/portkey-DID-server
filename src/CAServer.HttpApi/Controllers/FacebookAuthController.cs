using System.Threading.Tasks;
using CAServer.Facebook;
using CAServer.Facebook.Dtos;
using CAServer.Verifier;
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

    public FacebookAuthController(IFacebookAuthAppService facebookAuthAppService,
        IOptionsSnapshot<FacebookOptions> facebookOptions, ILogger<FacebookAuthController> logger)
    {
        _facebookAuthAppService = facebookAuthAppService;
        _logger = logger;
        _facebookOptions = facebookOptions.Value;
    }

    [HttpGet("receive")]
    public async Task<IActionResult> ReceiveAsync(string code)
    {
        var result = await _facebookAuthAppService.ReceiveAsync(code, ApplicationType.Receive);
        var redirectUrl = _facebookOptions.FacebookAuthUrl + "/portkey-auth-callback?userId=" + result.UserId + "&token=" +
                          result.AccessToken + "&expiresTime=" + result.ExpiresTime + "&type=Facebook";
        _logger.LogInformation("RedirectUrl is {url}: ", redirectUrl);
        return Redirect(redirectUrl);
    }

    [HttpGet("unifyReceive")]
    public async Task<IActionResult> UnifyReceiveAsync(string code)
    {
        var result = await _facebookAuthAppService.ReceiveAsync(code, ApplicationType.UnifyReceive);
        var redirectUrl = _facebookOptions.FacebookAuthUrl + "/auth-callback?userId=" + result.UserId + "&token=" +
                          result.AccessToken + "&expiresTime=" + result.ExpiresTime + "&type=Facebook";
        _logger.LogInformation("RedirectUrl is {url}: ", redirectUrl);
        return Redirect(redirectUrl);
    }
}