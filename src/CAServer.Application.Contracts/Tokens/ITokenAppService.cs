using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Tokens.Dtos;
using Volo.Abp.Application.Dtos;

namespace CAServer.Tokens;

public interface ITokenAppService
{
    Task<ListResultDto<TokenPriceDataDto>> GetTokenPriceListAsync(List<string> symbols);
    Task<ListResultDto<TokenPriceDataDto>> GetTokenHistoryPriceDataAsync(List<GetTokenHistoryPriceInput> inputs);
}