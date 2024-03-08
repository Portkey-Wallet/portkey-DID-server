using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Signature.Options;
using CAServer.Signature.Provider;
using CAServer.Tokens.TokenPrice;
using CoinGecko.Clients;
using CoinGecko.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
        var apiKey = AsyncHelper.RunSync(() =>
            _secretProvider.GetSecretWithCacheAsync(_signatureOptions.CurrentValue.KeyIds.CoinGecko));
        var httpClient = httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        if (_coinGeckoOptions.CurrentValue.BaseUrl.NotNullOrEmpty())
        {
            httpClient.BaseAddress = new Uri(_coinGeckoOptions.CurrentValue.BaseUrl);
        }

        if ((_coinGeckoOptions.CurrentValue.BaseUrl ?? "").Contains("pro"))
        {
            httpClient.DefaultRequestHeaders.Add("x-cg-pro-api-key", apiKey);
        }

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
}