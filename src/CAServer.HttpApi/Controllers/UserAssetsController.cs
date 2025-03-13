using System;
using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.Awaken;
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
            ? await _tokenNftAppService.GetTokenAsync(requestDto)
            : await _tokenDisplayAppService.GetTokenAsync(requestDto);
    }

    [HttpGet("awaken/token")]
    public async Task<AwakenSupportedTokenResponse> ListAwakenSupportedTokensAsync(int skipCount, int maxResultCount,
        int page, string chainId, string caAddress)
    {
        skipCount = skipCount <= 0 ? 0 : skipCount;
        maxResultCount = maxResultCount <= 0 ? 100 : maxResultCount;
        page = page <= 1 ? 1 : page;
        return await _tokenNftAppService.ListAwakenSupportedTokensAsync(skipCount, maxResultCount, page, chainId, caAddress);
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
    
    [HttpGet("setTraitsPercentage"), AllowAnonymous]
    public async Task<NftItem> SetTraitsPercentageAsync(string traits)
    {
        return await _userAssetsAppService.SetTraitsPercentageAsync(traits);
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
        var version = _httpContextAccessor.HttpContext?.Request.Headers["version"].ToString();
        return VersionContentHelper.CompareVersion(version, CommonConstant.NftToFtStartVersion)
            ? await _tokenNftAppService.SearchUserAssetsAsync(requestDto)
            : await _userAssetsAppService.SearchUserAssetsAsync(requestDto);
    }

    [HttpPost("searchUserPackageAssets")]
    public async Task<SearchUserPackageAssetsDto> SearchUserPackageAssetsAsync(
        SearchUserPackageAssetsRequestDto requestDto)
    {
        var version = _httpContextAccessor.HttpContext?.Request.Headers["version"].ToString();
        var userPackageAssets =  VersionContentHelper.CompareVersion(version, CommonConstant.NftToFtStartVersion)
            ? await _tokenNftAppService.SearchUserPackageAssetsAsync(requestDto)
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
    
    [AllowAnonymous]
    [HttpGet("asset-estimation")]
    public async Task<bool> AssetEstimation(UserAssetEstimationRequestDto request)
    {
        return await _userAssetsAppService.UserAssetEstimationAsync(request);
    }
}