using System;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Grains.Grain.Tokens.TokenPrice;
using CoinGecko.Clients;
using CoinGecko.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.CoinGeckoApi;

public class TokenPriceProvider : ITokenPriceProvider, ITransientDependency
{
    private readonly ICoinGeckoClient _coinGeckoClient;
    private readonly IRequestLimitProvider _requestLimitProvider;
    private readonly CoinGeckoOptions _coinGeckoOptions;

    private const string UsdSymbol = "usd";

    public ILogger<TokenPriceProvider> Logger { get; set; }

    public TokenPriceProvider(IRequestLimitProvider requestLimitProvider, IOptionsSnapshot<CoinGeckoOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _requestLimitProvider = requestLimitProvider;
        _coinGeckoClient = new CoinGeckoClient(httpClientFactory.CreateClient());
        _coinGeckoOptions = options.Value;
        Logger = NullLogger<TokenPriceProvider>.Instance;
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
            Logger.LogWarning($"can not get the token {symbol}");
            return 0;
        }

        try
        {
            var coinData =
                await RequestAsync(async () =>
                    await _coinGeckoClient.SimpleClient.GetSimplePrice(new[] { coinId }, new[] { UsdSymbol }));

            if (!coinData.TryGetValue(coinId, out var value))
            {
                return 0;
            }

            return value[UsdSymbol].Value;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"can not get current price :{symbol}.");
            throw;
        }
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
            Logger.LogWarning($"can not get the token {symbol}");
            return 0;
        }

        try
        {
            var coinData =
                await RequestAsync(async () => await _coinGeckoClient.CoinsClient.GetHistoryByCoinId(coinId,
                    dateTime, "false"));

            if (coinData.MarketData == null)
            {
                return 0;
            }

            return (decimal)coinData.MarketData.CurrentPrice[UsdSymbol].Value;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"can not get :{symbol} price.");
            throw;
        }
    }

    private string GetCoinIdAsync(string symbol)
    {
        return _coinGeckoOptions.CoinIdMapping.TryGetValue(symbol.ToUpper(), out var id) ? id : null;
    }

    private async Task<T> RequestAsync<T>(Func<Task<T>> task)
    {
        await _requestLimitProvider.RecordRequestAsync();
        return await task();
    }
}