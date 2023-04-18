using System.Threading.Tasks;
using CAServer.UserAssets.Dtos;

namespace CAServer.UserAssets;

public interface IUserAssetsAppService
{
    Task<GetTokenDto> GetTokenAsync(GetTokenRequestDto requestDto);

    Task<GetNFTProtocolsDto> GetNFTProtocolsAsync(GetNFTProtocolsRequestDto requestDto);

    Task<GetNFTItemsDto> GetNFTItemsAsync(GetNftItemsRequestDto requestDto);
    Task<GetRecentTransactionUsersDto> GetRecentTransactionUsersAsync(GetRecentTransactionUsersRequestDto requestDto);
    Task<SearchUserAssetsDto> SearchUserAssetsAsync(SearchUserAssetsRequestDto requestDto);
}