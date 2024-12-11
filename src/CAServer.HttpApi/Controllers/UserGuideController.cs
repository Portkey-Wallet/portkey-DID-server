using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.UserGuide;
using CAServer.UserGuide.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Guide")]
[Route("api/app/user/guide")]
[Authorize]
public class UserGuideController : CAServerController
{
    private readonly IUserGuideAppService _userGuideAppService;

    public UserGuideController(IUserGuideAppService userGuideAppService)
    {
        _userGuideAppService = userGuideAppService;
    }

    [HttpGet("list")]
    public async Task<UserGuideDto> ListUserGuideAsync()
    {
        return await _userGuideAppService.ListUserGuideAsync(CurrentUser.Id);
    }

    [HttpGet("query")]
    public async Task<UserGuideDto> QueryUserGuideAsync(
        UserGuideRequestDto input)
    {
        return await _userGuideAppService.QueryUserGuideAsync(input, CurrentUser.Id);
    }

    [HttpPost("finish")]
    public async Task<bool> FinishUserGuideAsync(
        UserGuideFinishRequestDto input)
    {
        return await _userGuideAppService.FinishUserGuideAsync(input,CurrentUser.Id);
    }
}