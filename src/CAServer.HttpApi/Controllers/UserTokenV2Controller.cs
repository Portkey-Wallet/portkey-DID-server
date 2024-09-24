using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Tokens;
using CAServer.Tokens.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("UserTokenV2")]
[Route("api/app/v2/userTokens")]
public class UserTokenV2Controller: CAServerController
{
    private readonly IUserTokenV2AppService _userTokenAppService;

    public UserTokenV2Controller(IUserTokenV2AppService userTokenAppService)
    {
        _userTokenAppService = userTokenAppService;
    }

    [HttpPost]
    [Route("display")]
    [Authorize]
    public async Task ChangeTokenDisplayAsync(ChangeTokenDisplayDto input)
    {
        await _userTokenAppService.ChangeTokenDisplayAsync(input);
    }

    [HttpGet, Authorize]
    public async Task<CaPageResultDto<GetUserTokenV2Dto>> GetTokensAsync(GetTokenInfosV2RequestDto requestDto)
    {
        return await _userTokenAppService.GetTokensAsync(requestDto);
    }
    
    [Authorize, HttpGet("/api/app/v2/tokens/list")]
    public async Task<CaPageResultDto<GetTokenListV2Dto>> GetTokenListAsync(GetTokenListV2RequestDto requestDto)
    {
        return await _userTokenAppService.GetTokenListAsync(requestDto);
    } 
    
}