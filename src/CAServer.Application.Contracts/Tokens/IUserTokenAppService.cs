using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.CAActivity.Dto;
using Volo.Abp.Application.Dtos;

namespace CAServer.Tokens
{
    public interface IUserTokenAppService
    {
        Task<PagedResultDto<UserTokenDto>> GetUserTokenListAsync(GetUserTokenListInput input);
        Task ChangeTokenDisplayAsync(bool isDisplay, Guid id);

        Task AddUserTokenAsync(Guid userId);

        // Task<ListResultDto<TokenDto>> GetTokenAsync(string flag);
        // Task<TokenPriceDataDto> GetTokenHistoryPriceDataAsync(string symbol, DateTime dateTime);
        // Task UpdateTokenPriceUsdAsync();
        // Task InitialToken();

    }
}

