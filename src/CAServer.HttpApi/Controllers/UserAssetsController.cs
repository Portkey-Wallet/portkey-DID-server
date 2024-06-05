using System;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Tokens;
using CAServer.UserAssets;
using CAServer.UserAssets.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("UserAssets")]
[Route("api/app/user/assets")]
[Authorize]
public class UserAssetsController
{
    private readonly IUserAssetsAppService _userAssetsAppService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenDisplayAppService _tokenDisplayAppService;
    private readonly ITokenNftAppService _tokenNftAppService;

    public UserAssetsController(IUserAssetsAppService userAssetsAppService, IHttpContextAccessor httpContextAccessor,
        ITokenDisplayAppService tokenDisplayAppService, ITokenNftAppService tokenNftAppService)
    {
        _userAssetsAppService = userAssetsAppService;
        _httpContextAccessor = httpContextAccessor;
        _tokenDisplayAppService = tokenDisplayAppService;
        _tokenNftAppService = tokenNftAppService;
    }

    [HttpPost("token")]
    public async Task<GetTokenDto> GetTokenAsync(GetTokenRequestDto requestDto)
    {
        var version = _httpContextAccessor.HttpContext?.Request.Headers["version"].ToString();
        return VersionContentHelper.CompareVersion(version, CommonConstant.NftToFtStartVersion)
            ? await _tokenDisplayAppService.GetTokenAsync(requestDto)
            : await _tokenNftAppService.GetTokenAsync(requestDto);
    }

    [HttpPost("nftCollections")]
    public async Task<GetNftCollectionsDto> GetNFTCollectionsAsync(GetNftCollectionsRequestDto requestDto)
    {
        return await _userAssetsAppService.GetNFTCollectionsAsync(requestDto);
    }

    [HttpPost("nftItems")]
    public async Task<GetNftItemsDto> GetNFTItemsAsync(GetNftItemsRequestDto requestDto)
    {
        return await _userAssetsAppService.GetNFTItemsAsync(requestDto);
    }

    [HttpPost("nftItem")]
    public async Task<NftItem> GetNFTItemAsync(GetNftItemRequestDto requestDto)
    {
        return await _userAssetsAppService.GetNFTItemAsync(requestDto);
    }

    [HttpPost("recentTransactionUsers")]
    public async Task<GetRecentTransactionUsersDto> GetRecentTransactionUsersAsync(
        GetRecentTransactionUsersRequestDto requestDto)
    {
        return await _userAssetsAppService.GetRecentTransactionUsersAsync(requestDto);
    }

    [HttpPost("searchUserAssets")]
    public async Task<SearchUserAssetsDto> SearchUserAssetsAsync(SearchUserAssetsRequestDto requestDto)
    {
        return await _userAssetsAppService.SearchUserAssetsAsync(requestDto);
    }

    [HttpPost("searchUserPackageAssets")]
    public async Task<SearchUserPackageAssetsDto> SearchUserPackageAssetsAsync(
        SearchUserPackageAssetsRequestDto requestDto)
    {
        var headers = _httpContextAccessor?.HttpContext?.Request.Headers;
        var version = headers != null && headers.ContainsKey("version") ? (string)headers["version"] : string.Empty;

        var userPackageAssets = requestDto.Version.IsNullOrEmpty()
            ? await _userAssetsAppService.SearchUserPackageAssetsAsync(requestDto)
            : await _tokenDisplayAppService.SearchUserPackageAssetsAsync(requestDto);

        return VersionContentHelper.FilterUserPackageAssetsByVersion(version, userPackageAssets);
    }


    [AllowAnonymous]
    [HttpGet("symbolImages")]
    public SymbolImagesDto GetSymbolImagesAsync()
    {
        return _userAssetsAppService.GetSymbolImagesAsync();
    }

    [AllowAnonymous]
    [HttpGet("tokenBalance")]
    public async Task<TokenInfoDto> GetTokenBalanceAsync(GetTokenBalanceRequestDto requestDto)
    {
        return await _userAssetsAppService.GetTokenBalanceAsync(requestDto);
    }
}