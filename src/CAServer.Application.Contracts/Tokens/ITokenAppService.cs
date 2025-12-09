using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Tokens.Dtos;
using CAServer.UserAssets;
using Volo.Abp.Application.Dtos;

namespace CAServer.Tokens;

public interface ITokenAppService
{
    Task<ListResultDto<TokenPriceDataDto>> GetTokenPriceListAsync(List<string> symbols);
    Task<ListResultDto<TokenPriceDataDto>> GetTokenHistoryPriceDataAsync(List<GetTokenHistoryPriceInput> inputs);
    Task<ContractAddressDto> GetContractAddressAsync();
    Task<List<GetTokenListDto>> GetTokenListAsync(GetTokenListRequestDto input);
    Task<GetTokenInfoDto> GetTokenInfoAsync(string chainId, string symbol);
    Task<TokenExchange> GetAvgLatestExchangeAsync(string fromSymbol, string toSymbol);
    Task<TokenExchange> GetLatestExchangeAsync(string providerName, string fromSymbol, string toSymbol);
    Task<TokenExchange> GetHistoryExchangeAsync(string providerName, string fromSymbol, string toSymbol, DateTime timestamp);

    Task<GetTokenAllowancesDto> GetTokenAllowancesAsync(GetAssetsBase input);
}