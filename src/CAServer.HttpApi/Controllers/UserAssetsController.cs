using System.Threading.Tasks;
using CAServer.UserAssets;
using CAServer.UserAssets.Dtos;
using Microsoft.AspNetCore.Authorization;
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

    public UserAssetsController(IUserAssetsAppService userAssetsAppService)
    {
        _userAssetsAppService = userAssetsAppService;
    }

    [HttpPost("token")]
    public async Task<GetTokenDto> GetTokenAsync(GetTokenRequestDto requestDto)
    {
        return await _userAssetsAppService.GetTokenAsync(requestDto);
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

    [AllowAnonymous]
    [HttpGet("symbolImages")]
    public SymbolImagesDto GetSymbolImagesAsync()
    {
        return _userAssetsAppService.GetSymbolImagesAsync();
    }
}