using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Awaken;
using CAServer.Tokens.Dtos;
using CAServer.UserAssets;
using CAServer.UserAssets.Dtos;

namespace CAServer.Tokens;

public interface ITokenDisplayAppService
{
    Task<GetTokenDto> GetTokenAsync(GetTokenRequestDto requestDto);
    Task<List<GetTokenListDto>> GetTokenListAsync(GetTokenListRequestDto input);
    Task<SearchUserPackageAssetsDto> SearchUserPackageAssetsAsync(SearchUserPackageAssetsRequestDto requestDto);

    Task<AwakenSupportedTokenResponse> ListAwakenSupportedTokensAsync(int skipCount, int maxResultCount,
        int page, string chainId);
}