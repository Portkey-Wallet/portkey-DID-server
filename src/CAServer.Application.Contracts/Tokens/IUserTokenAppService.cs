using System;
using System.Threading.Tasks;
using CAServer.Tokens.Dtos;

namespace CAServer.Tokens
{
    public interface IUserTokenAppService
    {
        Task<UserTokenDto> ChangeTokenDisplayAsync(bool isDisplay, Guid id);
        Task<UserTokenDto> AddUserTokenAsync(Guid userId,AddUserTokenInput input);
    }
}