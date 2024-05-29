using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Market;
using CAServer.Signature.Options;
using CAServer.Signature.Provider;
using CoinGecko.Clients;
using CoinGecko.Entities.Response.Coins;
using CoinGecko.Entities.Response.Search;
using CoinGecko.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace CAServer.CoinGeckoApi;

public class MarketDataProvider : IMarketDataProvider, ITransientDependency
{
    private readonly ILogger<MarketDataProvider> _logger;
    private readonly ICoinGeckoClient _coinGeckoClient;
    private readonly IRequestLimitProvider _requestLimitProvider;
    private readonly IOptionsMonitor<CoinGeckoOptions> _coinGeckoOptions;
    private readonly IOptionsMonitor<SignatureServerOptions> _signatureOptions;
    private readonly ISecretProvider _secretProvider;

    private const string UsdSymbol = "usd";

    public MarketDataProvider(IRequestLimitProvider requestLimitProvider, IOptionsMonitor<CoinGeckoOptions> options,
        IHttpClientFactory httpClientFactory, ISecretProvider secretProvider,
        IOptionsMonitor<SignatureServerOptions> signatureOptions, ILogger<MarketDataProvider> logger)
    {
        _requestLimitProvider = requestLimitProvider;
        _coinGeckoOptions = options;
        _secretProvider = secretProvider;
        _signatureOptions = signatureOptions;
        _logger = logger;
        _coinGeckoClient = new CoinGeckoClient(InitCoinGeckoClient(httpClientFactory));
    }
    
    private HttpClient InitCoinGeckoClient(IHttpClientFactory httpClientFactory)
    {
        var apiKey = AsyncHelper.RunSync(() =>
            _secretProvider.GetSecretWithCacheAsync(_signatureOptions.CurrentValue.KeyIds.CoinGecko));
        var httpClient = httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        if (_coinGeckoOptions.CurrentValue.Timeout > 0)
        {
            httpClient.Timeout = TimeSpan.FromMilliseconds(_coinGeckoOptions.CurrentValue.Timeout);
        }

        if (_coinGeckoOptions.CurrentValue.BaseUrl.NotNullOrEmpty())
        {
            httpClient.BaseAddress = new Uri(_coinGeckoOptions.CurrentValue.BaseUrl);
        }

        if ((_coinGeckoOptions.CurrentValue.BaseUrl ?? "").Contains("pro"))
        {
            httpClient.DefaultRequestHeaders.Add("x-cg-pro-api-key", apiKey);
        }
        else if (!_coinGeckoOptions.CurrentValue.DemoApiKey.IsNullOrWhiteSpace())
        {
            // test environment uses the demo api-key
            httpClient.DefaultRequestHeaders.Add("x-cg-demo-api-key", _coinGeckoOptions.CurrentValue.DemoApiKey);
        }

        return httpClient;
    }
    
    public async Task<List<CoinMarkets>> GetHotListingsAsync()
    {
        List<CoinMarkets> response;
        try
        {
            response = await _coinGeckoClient.CoinsClient.GetCoinMarkets(UsdSymbol);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Can not get hot listings");
            throw;
        }

        return response;
    }

    public async Task<List<CoinMarkets>> GetCoinMarketsByCoinIdsAsync(string[] ids, int perPage)
    {
        List<CoinMarkets> response;
        try
        {
            response = await _coinGeckoClient.CoinsClient.GetCoinMarkets(UsdSymbol, ids, "market_cap_desc", perPage, 1, false, "24h", "layer-1");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Can not get hot listings");
            throw;
        }

        return response;
    }

    public async Task<TrendingList> GetTrendingListingsAsync()
    {
        TrendingList trendingList;
        try
        {
            trendingList = await _coinGeckoClient.SearchClient.GetSearchTrending();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Can not get trending listings");
            throw;
        }

        return trendingList;
    }

    public async Task<MarketChartById> GetMarketChartsByCoinIdAsync(string coinId, string vsCurrency, string days)
    {
        MarketChartById marketChartById;
        try
        {
            marketChartById = await _coinGeckoClient.CoinsClient.GetMarketChartsByCoinId(coinId, vsCurrency, days);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Can not get market charts by coin id, coinId={0}, vsCurrency={1}, days={2}", coinId, vsCurrency, days);
            throw;
        }
        return marketChartById;
    }
}