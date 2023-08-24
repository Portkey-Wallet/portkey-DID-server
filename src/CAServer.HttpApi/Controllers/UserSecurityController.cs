using System.Threading.Tasks;
using CAServer.Security;
using CAServer.Security.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Security")]
[Route("api/app/user/security")]
// [Authorize]
public class UserSecurityController : CAServerController
{
    private readonly IUserSecurityAppService _userSecurityAppService;

    public UserSecurityController(IUserSecurityAppService userSecurityAppService)
    {
        _userSecurityAppService = userSecurityAppService;
    }

    [HttpGet("selfTest")]
    public async Task<UserSecuritySelfTestResultDto> GetUserSecuritySelfTestAsync(GetUserSecuritySelfTestDto input)
    {
        return await _userSecurityAppService.GetUserSecuritySelfTestAsync(input);
    }
}