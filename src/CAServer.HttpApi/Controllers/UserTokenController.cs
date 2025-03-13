using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.Commons;
using CAServer.Models;
using CAServer.Tokens;
using CAServer.Tokens.Dtos;
using CAServer.UserAssets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("UserToken")]
[Route("api/app/userTokens")]
public class UserTokenController : CAServerController
{
    private readonly IUserTokenAppService _userTokenAppService;

    public UserTokenController(IUserTokenAppService userTokenService)
    {
        _userTokenAppService = userTokenService;
    }

    [HttpPut]
    [Route("{id}/display")]
    [Authorize]
    public async Task ChangeTokenDisplayAsync(string id, [FromBody] IsTokenDisplayInput input)
    {
        await _userTokenAppService.ChangeTokenDisplayAsync(input.IsDisplay, id);
    }

    [HttpGet, Authorize]
    public async Task<PagedResultDto<GetUserTokenDto>> GetTokensAsync(GetTokenInfosRequestDto requestDto)
    {
        return await _userTokenAppService.GetTokensAsync(requestDto);
    }
}