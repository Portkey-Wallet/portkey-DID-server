using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.Provider;
using Volo.Abp.Application.Dtos;

namespace CAServer.Tokens;

public interface ITokenAppService
{
    Task<ListResultDto<TokenPriceDataDto>> GetTokenPriceListAsync(List<string> symbols);
    Task<ListResultDto<TokenPriceDataDto>> GetTokenHistoryPriceDataAsync(List<GetTokenHistoryPriceInput> inputs);

    Task<ContractAddressDto> GetContractAddressAsync();

    Task<List<GetTokenListDto>> GetTokenListAsync(GetTokenListRequestDto input);
    Task<GetTokenInfoDto> GetTokenInfoAsync(string chainId, string symbol);

    Task<TokenExchange> GetLatestExchange(ExchangeProviderName provider, string fromSymbol, string toSymbol);

    Task<TokenExchange> GetHistoryExchange(ExchangeProviderName provider, string fromSymbol, string toSymbol, DateTime timestamp);

}