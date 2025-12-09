using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.PrivacyPolicy;
using CAServer.PrivacyPolicy.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("PrivacyPolicy")]
[Route("api/app/privacypolicy")]
[Authorize]
public class PrivacyPolicyController : CAServerController
{
    private readonly IPrivacyPolicyAppService _privacyPolicyAppService;

    public PrivacyPolicyController(IPrivacyPolicyAppService privacyPolicyAppService)
    {
        _privacyPolicyAppService = privacyPolicyAppService;
    }

    [HttpPost("sign")]
    public async Task SingleAsync(PrivacyPolicySignDto input)
    {
        await _privacyPolicyAppService.SignAsync(input);
    }
}