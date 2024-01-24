using System;
using System.Threading.Tasks;
using CAServer.Security.Dtos;
using CAServer.UserGuide;
using CAServer.UserGuide.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Guide")]
[Route("api/app/user/guide")]
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
        var userId = Guid.NewGuid();
        //return await _userGuildAppService.ListUserGuideAsync(CurrentUser.Id);
        return await _userGuideAppService.ListUserGuideAsync(userId);
    }

    [HttpGet("query")]
    public async Task<UserGuideDto> QueryUserGuideAsync(
        UserGuideRequestDto input)
    {
        var userId = Guid.NewGuid();
        return await _userGuideAppService.QueryUserGuideAsync(input, userId);
        //return await _userGuildAppService.QueryUserGuideAsync(input, CurrentUser.Id);
    }

    [HttpPost("finish")]
    public async Task<bool> FinishUserGuideAsync(
        UserGuideFinishRequestDto input)
    {
        var userId = Guid.NewGuid();
        return await _userGuideAppService.FinishUserGuideAsync(input, userId);
        //return await _userGuildAppService.FinishUserGuideAsync(input,CurrentUser.Id);
    }
}