using System.Threading.Tasks;
using CAServer.AppleAuth;
using CAServer.AppleAuth.Dtos;
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
        return Redirect($"{_appleAuthOptions.RedirectUrl}?id_token={appleAuthDto.Id_token}");
    }
}