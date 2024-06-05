using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Tokens.Dtos;
using CAServer.UserAssets;
using CAServer.UserAssets.Dtos;

namespace CAServer.Tokens;

public interface ITokenNftAppService
{
    Task<GetTokenDto> GetTokenAsync(GetTokenRequestDto requestDto);
    Task<List<GetTokenListDto>> GetTokenListAsync(GetTokenListRequestDto input);
    Task<SearchUserPackageAssetsDto> SearchUserPackageAssetsAsync(SearchUserPackageAssetsRequestDto requestDto);
    Task<SearchUserAssetsDto> SearchUserAssetsAsync(SearchUserAssetsRequestDto requestDto);
}