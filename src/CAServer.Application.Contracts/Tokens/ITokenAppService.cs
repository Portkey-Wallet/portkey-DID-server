using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace CAServer.Tokens;

public interface ITokenAppService
{
    Task<ListResultDto<TokenPriceDataDto>> GetTokenPriceListAsync(List<string> symbols);
    Task<TokenPriceDataDto> GetTokenHistoryPriceDataAsync(string symbol, DateTime dateTime);
}