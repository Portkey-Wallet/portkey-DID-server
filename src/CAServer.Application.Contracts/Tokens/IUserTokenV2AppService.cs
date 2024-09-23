using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Tokens.Dtos;

namespace CAServer.Tokens;

public interface IUserTokenV2AppService
{
    Task ChangeTokenDisplayAsync(ChangeTokenDisplayDto requestDto);
    Task<CaPageResultDto<GetUserTokenV2Dto>> GetTokensAsync(GetTokenInfosV2RequestDto requestDto);
    Task<CaPageResultDto<GetTokenListV2Dto>> GetTokenListAsync(GetTokenListV2RequestDto requestDto);
}