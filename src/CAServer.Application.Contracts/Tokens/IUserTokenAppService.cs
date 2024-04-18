using System;
using System.Threading.Tasks;
using CAServer.Tokens.Dtos;
using Volo.Abp.Application.Dtos;

namespace CAServer.Tokens
{
    public interface IUserTokenAppService
    {
        Task<UserTokenDto> ChangeTokenDisplayAsync(bool isDisplay, string id);
        Task<UserTokenDto> AddUserTokenAsync(Guid userId,AddUserTokenInput input);
        Task<PagedResultDto<GetUserTokenDto>> GetTokensAsync(GetTokenInfosRequestDto requestDto);
    }
}