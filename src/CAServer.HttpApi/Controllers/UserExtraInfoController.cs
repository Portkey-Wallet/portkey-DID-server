using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.UserExtraInfo;
using CAServer.UserExtraInfo.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("UserExtraInfo")]
[Route("api/app/userExtraInfo")]
public class CAUserInfoController: CAServerController
{
    private readonly IUserExtraInfoAppService _userExtraInfoAppService;

    public CAUserInfoController(IUserExtraInfoAppService userExtraInfoAppService)
    {
        _userExtraInfoAppService = userExtraInfoAppService;
    }

    [HttpPost("appleUserExtraInfo")]
    public async Task<AddAppleUserExtraInfoResultDto> AddAppleUserExtraInfoAsync(AddAppleUserExtraInfoDto extraInfoDto)
    {
        return await _userExtraInfoAppService.AddAppleUserExtraInfoAsync(extraInfoDto);
    }
    
    [HttpGet("{id}")]
    public async Task<UserExtraInfoResultDto> GetUserExtraInfoAsync(string id)
    {
        return await _userExtraInfoAppService.GetUserExtraInfoAsync(id);
    }
    
}