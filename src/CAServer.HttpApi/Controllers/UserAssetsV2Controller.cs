using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Tokens;
using CAServer.UserAssets;
using CAServer.UserAssets.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using TokenHelper = CAServer.Common.TokenHelper;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("UserAssets")]
[Route("api/app/v2/user/assets")]
[Authorize]
public class UserAssetsV2Controller
{
    private readonly IUserAssetsAppService _userAssetsAppService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenDisplayAppService _tokenDisplayAppService;
    private readonly ITokenNftAppService _tokenNftAppService;

    public UserAssetsV2Controller(IUserAssetsAppService userAssetsAppService, IHttpContextAccessor httpContextAccessor,
        ITokenDisplayAppService tokenDisplayAppService, ITokenNftAppService tokenNftAppService)
    {
        _userAssetsAppService = userAssetsAppService;
        _httpContextAccessor = httpContextAccessor;
        _tokenDisplayAppService = tokenDisplayAppService;
        _tokenNftAppService = tokenNftAppService;
    }

    [HttpPost("token")]
    public async Task<GetTokenV2Dto> GetTokenAsync(GetTokenRequestDto requestDto)
    {
        requestDto.MaxResultCount = requestDto.MaxResultCount * 2;
        requestDto.SkipCount = requestDto.SkipCount * 2;
        var version = _httpContextAccessor.HttpContext?.Request.Headers["version"].ToString();
        var dto = VersionContentHelper.CompareVersion(version, CommonConstant.NftToFtStartVersion)
            ? await _tokenNftAppService.GetTokenAsync(requestDto)
            : await _tokenDisplayAppService.GetTokenAsync(requestDto);
        var result = TokenHelper.ConvertFromGetToken(dto);
        return result;
    }
    
    [HttpPost("searchUserAssets")]
    public async Task<SearchUserAssetsV2Dto> SearchUserAssetsAsync(SearchUserAssetsRequestDto requestDto)
    {
        var version = _httpContextAccessor.HttpContext?.Request.Headers["version"].ToString();
        var searchDto = VersionContentHelper.CompareVersion(version, CommonConstant.NftToFtStartVersion)
            ? await _tokenNftAppService.SearchUserAssetsAsync(requestDto)
            : await _userAssetsAppService.SearchUserAssetsAsync(requestDto);
        return await _userAssetsAppService.SearchUserAssetsAsyncV2(requestDto, searchDto);
    }
}