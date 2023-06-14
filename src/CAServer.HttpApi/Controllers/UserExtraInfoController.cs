using System.Threading.Tasks;
using CAServer.Response;
using CAServer.UserExtraInfo;
using CAServer.UserExtraInfo.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("UserExtraInfo")]
[Route("api/app/userExtraInfo")]
public class CAUserInfoController : CAServerController
{
    private readonly IUserExtraInfoAppService _userExtraInfoAppService;

    public CAUserInfoController(IUserExtraInfoAppService userExtraInfoAppService)
    {
        _userExtraInfoAppService = userExtraInfoAppService;
    }

    [HttpPost("appleUserExtraInfo")]
    public IActionResult AddAppleUserExtraInfoAsync(AddAppleUserExtraInfoDto extraInfoDto)
    {
        return Ok(new ResponseDto()
        {
            Data = new AppleUserInfoDto()
            {
                Name = new AppleUserName()
                {
                    FirstName = "aaaa",
                    LastName = "bbb"
                }
            }
        });
        //return await _userExtraInfoAppService.AddAppleUserExtraInfoAsync(extraInfoDto);
    }

    [HttpGet("{id}")]
    public async Task<UserExtraInfoResultDto> GetUserExtraInfoAsync(string id)
    {
        return await _userExtraInfoAppService.GetUserExtraInfoAsync(id);
    }
}