﻿using System;
using CAServer.CAAccount;
using CAServer.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("NickName")]
[Route("api/app/account/nickname")]
[Authorize]
public class NickNameController : CAServerController
{
    private readonly INickNameAppService _nickNameService;

    public NickNameController(INickNameAppService nickNameService)
    {
        _nickNameService = nickNameService;
    }

    [HttpPut]
    public async Task<CAHolderResultDto> NicknameAsync([FromBody] UpdateNickNameDto nickNameDto)
    {
        return await _nickNameService.SetNicknameAsync(nickNameDto);
    }
}