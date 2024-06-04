using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Signature.Options;
using CAServer.Signature.Provider;
using CAServer.Tokens.TokenPrice;
using CoinGecko.Clients;
using CoinGecko.Entities.Response.Coins;
using CoinGecko.Entities.Response.Search;
using CoinGecko.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace CAServer.CoinGeckoApi;

public class TokenPriceProvider : ITokenPriceProvider, ITransientDependency
{
    private readonly ILogger<TokenPriceProvider> _logger;
    private readonly ICoinGeckoClient _coinGeckoClient;
    private readonly IRequestLimitProvider _requestLimitProvider;
    private readonly IOptionsMonitor<CoinGeckoOptions> _coinGeckoOptions;
    private readonly IOptionsMonitor<SignatureServerOptions> _signatureOptions;
    private readonly ISecretProvider _secretProvider;

    private const string UsdSymbol = "usd";

    public TokenPriceProvider(IRequestLimitProvider requestLimitProvider, IOptionsMonitor<CoinGeckoOptions> options,
        IHttpClientFactory httpClientFactory, ISecretProvider secretProvider,
        IOptionsMonitor<SignatureServerOptions> signatureOptions, ILogger<TokenPriceProvider> logger)
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
        // var apiKey = AsyncHelper.RunSync(() =>
        //     _secretProvider.GetSecretWithCacheAsync(_signatureOptions.CurrentValue.KeyIds.CoinGecko));
        // var apiKey = _coinGeckoOptions.CurrentValue.ProdApiKey;
        var apiKey = "CG-w7chQ539gL346ZAF8EMngh7k";
        var httpClient = httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        if (_coinGeckoOptions.CurrentValue.Timeout > 0)
        {
            httpClient.Timeout = TimeSpan.FromMilliseconds(_coinGeckoOptions.CurrentValue.Timeout);
        }

        if (_coinGeckoOptions.CurrentValue.BaseUrl.NotNullOrEmpty())
        {
            // httpClient.BaseAddress = new Uri(_coinGeckoOptions.CurrentValue.BaseUrl);
            httpClient.BaseAddress = new Uri("https://pro-api.coingecko.com/api/v3");
        }

        // if ((_coinGeckoOptions.CurrentValue.BaseUrl ?? "").Contains("pro"))
        // {
        httpClient.DefaultRequestHeaders.Add("x-cg-pro-api-key", apiKey);
        // }
        // else if (!_coinGeckoOptions.CurrentValue.DemoApiKey.IsNullOrWhiteSpace())
        // {
        //     // test environment uses the demo api-key
        //     httpClient.DefaultRequestHeaders.Add("x-cg-demo-api-key", _coinGeckoOptions.CurrentValue.DemoApiKey);
        // }

        return httpClient;
    }

    public async Task<decimal> GetPriceAsync(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            return 0;
        }

        var coinId = GetCoinIdAsync(symbol);
        if (coinId == null)
        {
            _logger.LogWarning("Can not get the token {Symbol}", symbol);
            return 0;
        }

        try
        {
            var coinData =
                await RequestAsync(async () =>
                    await _coinGeckoClient.SimpleClient.GetSimplePrice(new[] { coinId }, new[] { UsdSymbol }));
            _logger.LogDebug("Get coinGecko data: {Price}", JsonConvert.SerializeObject(coinData));
            if (!coinData.TryGetValue(coinId, out var value))
            {
                return 0;
            }

            return value[UsdSymbol].Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Can not get current price from 'CoinGecko' :{Symbol}", symbol);
            throw;
        }
    }

    public async Task<Dictionary<string, decimal>> GetPriceAsync(params string[] symbols)
    {
        if (symbols.IsNullOrEmpty())
        {
            return null;
        }

        var prices = new Dictionary<string, decimal>();
        foreach (var symbol in symbols)
        {
            var price = await GetPriceAsync(symbol);
            if (price != 0)
            {
                prices.Add(symbol, price);
            }
        }

        return prices;
    }

    public async Task<decimal> GetHistoryPriceAsync(string symbol, string dateTime)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            return 0;
        }

        var coinId = GetCoinIdAsync(symbol);
        if (coinId == null)
        {
            _logger.LogWarning("Can not get the token {Symbol}", symbol);
            return 0;
        }

        try
        {
            var coinData =
                await RequestAsync(async () => await _coinGeckoClient.CoinsClient.GetHistoryByCoinId(coinId,
                    dateTime, "false"));
            
            if (coinData.MarketData == null)
            {
                _logger.LogError("get history price error: {symbol}, {dateTime}", symbol, dateTime);
                return 0;
            }

            return (decimal)coinData.MarketData.CurrentPrice[UsdSymbol].Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Can not get :{Symbol} price", symbol);
            throw;
        }
    }

    public bool IsAvailable()
    {
        return _coinGeckoOptions.CurrentValue.IsAvailable;
    }

    public int GetPriority()
    {
        return _coinGeckoOptions.CurrentValue.Priority;
    }

    private string GetCoinIdAsync(string symbol)
    {
        return _coinGeckoOptions.CurrentValue.CoinIdMapping.TryGetValue(symbol.ToUpper(), out var id) ? id : null;
    }

    private async Task<T> RequestAsync<T>(Func<Task<T>> task)
    {
        await _requestLimitProvider.RecordRequestAsync();
        return await task();
    }
    
    public async Task<List<CoinMarkets>> GetHotListingsAsync()
    {
        List<CoinMarkets> response;
        try
        {
            response = await _coinGeckoClient.CoinsClient.GetCoinMarkets(UsdSymbol);
            // var options = new RestClientOptions("https://pro-api.coingecko.com/api/v3/coins/markets?vs_currency=usd");
            // var client = new RestClient(options);
            // var request = new RestRequest("");
            // request.AddHeader("accept", "application/json");
            // request.AddHeader("x-cg-pro-api-key", "CG-w7chQ539gL346ZAF8EMngh7k");
            // var responseFromApi = await client.GetAsync(request);
            // response = JsonConvert.DeserializeObject<List<CoinMarkets>>(responseFromApi.Content);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Can not get hot listings");
            throw;
        }
        DealWithSgrMarketCap(response);
        return response;
    }

    private static void DealWithSgrMarketCap(List<CoinMarkets> response)
    {
        foreach (var coinMarketse in response)
        {
            if (CommonConstant.SgrCoingeckoId.Equals(coinMarketse.Id) && coinMarketse.CurrentPrice.HasValue
                                                                      && (!coinMarketse.MarketCap.HasValue || Decimal.Compare(coinMarketse.MarketCap.Value, Decimal.Zero) == 0))
            {
                coinMarketse.MarketCap = Decimal.Multiply(21000000, (decimal)coinMarketse.CurrentPrice);
            }
        }
    }

    public async Task<List<CoinMarkets>> GetCoinMarketsByCoinIdsAsync(string[] ids, int perPage)
    {
        List<CoinMarkets> response;
        try
        {
            response = await _coinGeckoClient.CoinsClient.GetCoinMarkets(UsdSymbol, ids, "market_cap_desc", perPage, 1, false, "24h", "");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Can not get hot listings");
            throw;
        }
        DealWithSgrMarketCap(response);
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