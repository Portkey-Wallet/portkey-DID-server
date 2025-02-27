using System.Threading.Tasks;
using CAServer.Etos;
using CAServer.UserAssets.Dtos;
using Portkey.Contracts.CA;

namespace CAServer.UserAssets;

public interface IUserAssetsAppService
{
    Task<GetTokenDto> GetTokenAsync(GetTokenRequestDto requestDto);
    Task<GetNftCollectionsDto> GetNFTCollectionsAsync(GetNftCollectionsRequestDto requestDto);
    Task<GetNftItemsDto> GetNFTItemsAsync(GetNftItemsRequestDto requestDto);
    Task<NftItem> GetNFTItemAsync(GetNftItemRequestDto requestDto);
    Task<GetRecentTransactionUsersDto> GetRecentTransactionUsersAsync(GetRecentTransactionUsersRequestDto requestDto);
    Task<SearchUserAssetsDto> SearchUserAssetsAsync(SearchUserAssetsRequestDto requestDto);
    Task<SearchUserAssetsV2Dto> SearchUserAssetsAsyncV2(SearchUserAssetsRequestDto requestDto, SearchUserAssetsDto searchDto);
    Task<SearchUserPackageAssetsDto> SearchUserPackageAssetsAsync(SearchUserPackageAssetsRequestDto requestDto);
    SymbolImagesDto GetSymbolImagesAsync();
    Task<TokenInfoDto> GetTokenBalanceAsync(GetTokenBalanceRequestDto requestDto);

    Task NftTraitsProportionCalculateAsync();
    Task<bool> UserAssetEstimationAsync(UserAssetEstimationRequestDto request);

    Task<NftItem> SetTraitsPercentageAsync(string traits);
}