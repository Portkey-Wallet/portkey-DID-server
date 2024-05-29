using System.Collections.Generic;
using CAServer.Commons;
using Volo.Abp.DependencyInjection;

namespace CAServer.Market;

public interface IMarketRequestProvider : ISingletonDependency
{

    public CoinMarketCapResponseDto<List<CryptocurrencyExchangeInfoDto>> GetCryptocurrencyLogo(List<long> ids);

    public CoinMarketCapResponseDto<List<CryptocurrencyQuotesLatestDto>>
        GetCryptocurrencyQuotesLatest(List<string> ids);
    
    public CoinMarketCapResponseDto<List<CryptocurrencyListingsLatestDto>> GetCryptocurrencyListingsLatestAsync();

    public CoinMarketCapResponseDto<List<CryptocurrencyTrendingLatest>> GetCryptocurrencyTrendingLatestAsync();
}