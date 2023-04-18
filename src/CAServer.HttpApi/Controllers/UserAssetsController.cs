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

    [HttpPost("nftProtocols")]
    public async Task<GetNFTProtocolsDto> GetNFTProtocolsAsync(GetNFTProtocolsRequestDto requestDto)
    {
        return await _userAssetsAppService.GetNFTProtocolsAsync(requestDto);
    }

    [HttpPost("nftItems")]
    public async Task<GetNFTItemsDto> GetNFTItemsAsync(GetNftItemsRequestDto requestDto)
    {
        return await _userAssetsAppService.GetNFTItemsAsync(requestDto);
    }

    [HttpPost("recentTransactionUsers")]
    public async Task<GetRecentTransactionUsersDto> GetRecentTransactionUsersAsync(GetRecentTransactionUsersRequestDto requestDto)
    {
        return await _userAssetsAppService.GetRecentTransactionUsersAsync(requestDto);
    }

    [HttpPost("searchUserAssets")]
    public async Task<SearchUserAssetsDto> SearchUserAssetsAsync(SearchUserAssetsRequestDto requestDto)
    {
        return await _userAssetsAppService.SearchUserAssetsAsync(requestDto);
    }
}