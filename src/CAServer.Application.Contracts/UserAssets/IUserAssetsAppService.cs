using System.Threading.Tasks;
using CAServer.UserAssets.Dtos;
using Portkey.Contracts.CA;

namespace CAServer.UserAssets;

public interface IUserAssetsAppService
{
    Task<GetTokenDto> GetTokenAsync(GetTokenRequestDto requestDto);

    Task<GetNftCollectionsDto> GetNFTCollectionsAsync(GetNftCollectionsRequestDto requestDto);

    Task<GetNftItemsDto> GetNFTItemsAsync(GetNftItemsRequestDto requestDto);
    Task<GetRecentTransactionUsersDto> GetRecentTransactionUsersAsync(GetRecentTransactionUsersRequestDto requestDto);
    Task<SearchUserAssetsDto> SearchUserAssetsAsync(SearchUserAssetsRequestDto requestDto);
    SymbolImagesDto GetSymbolImagesAsync();


    Task<TokenInfoDto> GetTokenBalanceAsync(GetTokenBalanceRequestDto requestDto);
    
    Task CheckChainMerkerTreeStatusAsync(GetTokenRequestDto requestDto);
    Task UpdateMerkerTreeAsync(string chainId, string identifierHash, GetHolderInfoOutput result);
}