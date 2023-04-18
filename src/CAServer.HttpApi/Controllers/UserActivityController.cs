using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.CAActivity;
using CAServer.CAActivity.Dto;
using CAServer.CAActivity.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Controllers.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("UserActivity")]
[Route("api/app/user/activities")]
public class UserActivityController
{
    private readonly IUserActivityAppService _userActivityAppService;

    public UserActivityController(IUserActivityAppService userActivityAppService)
    {
        _userActivityAppService = userActivityAppService;
    }

    [HttpPost("activities")]
    public async Task<List<GetActivitiesDto>> GetActivitiesAsync(GetActivitiesRequestDto requestDto)
    {
        return await _userActivityAppService.GetActivitiesAsync(requestDto);
    }

    [HttpPost("activity")]
    public async Task<GetActivitiesDto> GetActivityAsync(GetActivityRequestDto requestDto)
    {
        return await _userActivityAppService.GetActivityAsync(requestDto);
    }
}