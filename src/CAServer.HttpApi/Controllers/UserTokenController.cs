using System;
using System.Threading.Tasks;
using CAServer.Models;
using CAServer.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

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
    
    
    [HttpPut]
    [Route("refreshToken")]
    public async Task RefreshTokenDataAsync()
    {
        await _userTokenAppService.RefreshTokenDataAsync();
    }
    
}