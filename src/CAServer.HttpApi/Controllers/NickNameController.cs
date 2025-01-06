using System;
using System.Collections.Generic;
using CAServer.CAAccount;
using CAServer.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("NickName")]
[Route("api/app/account")]
[Authorize]
public class NickNameController : CAServerController
{
    private readonly INickNameAppService _nickNameService;

    public NickNameController(INickNameAppService nickNameService)
    {
        _nickNameService = nickNameService;
    }

    [HttpPut("nickname")]
    public async Task<CAHolderResultDto> NicknameAsync([FromBody] UpdateNickNameDto nickNameDto)
    {
        return await _nickNameService.SetNicknameAsync(nickNameDto);
    }

    [HttpGet("caHolder")]
    public async Task<CAHolderResultDto> GetCaHolderAsync()
    {
        return await _nickNameService.GetCaHolderAsync();
    }
    
    [HttpPost("holderInfo")]
    public async Task<CAHolderResultDto> HolderInfoAsync(HolderInfoDto holderInfo)
    {
        return await _nickNameService.UpdateHolderInfoAsync(holderInfo);
    }
    
    [HttpPost("queryHolderInfos")]
    [AllowAnonymous]
    public async Task<List<CAHolderWithAddressResultDto>> QueryHolderInfosAsync(QueryUserInfosInput input)
    {
        return await _nickNameService.QueryHolderInfosAsync(input);
    }
    
    [HttpGet("defaultAvatars")]
    [AllowAnonymous]
    public DefaultAvatarResponse GetDefaultAvatars()
    {
        return _nickNameService.GetDefaultAvatars();
    }
    
    [HttpGet("poppedUp")]
    public async Task<bool> GetPoppedUpAccountAsync()
    {
        return await _nickNameService.GetPoppedUpAccountAsync();
    }
    
    [HttpGet("bubbling")]
    public async Task<bool> GetBubblingAccountAsync()
    {
        return await _nickNameService.GetBubblingAccountAsync();
    }
    
    [HttpPost("replace")]
    public async Task<string> ReplaceUserNicknameAsync([FromBody] ReplaceNicknameDto replaceNicknameDto)
    {
        await _nickNameService.ReplaceUserNicknameAsync(replaceNicknameDto);
        return "success";
    }
}