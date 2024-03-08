using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Options;
using CAServer.Tokens.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace CAServer.Tokens.TokenPrice;

public class TokenPriceService : ITokenPriceService, ISingletonDependency
{
    private readonly ILogger<TokenPriceService> _logger;
    private readonly IEnumerable<ITokenPriceProvider> _tokenPriceProviders;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly IOptionsMonitor<TokenPriceWorkerOption> _tokenPriceWorkerOption;
    private readonly SemaphoreSlim _lock;

    public TokenPriceService(ILogger<TokenPriceService> logger, IEnumerable<ITokenPriceProvider> tokenPriceProviders,
        IDistributedCache<string> distributedCache, IOptionsMonitor<TokenPriceWorkerOption> tokenPriceWorkerOption)
    {
        _logger = logger;
        
        if (tokenPriceProviders != null)
        {
            _tokenPriceProviders = tokenPriceProviders.OrderBy(provider => provider.GetPriority());
        }
        _distributedCache = distributedCache;
        _tokenPriceWorkerOption = tokenPriceWorkerOption;
        _lock = new SemaphoreSlim(1);
    }

    public async Task<TokenPriceGrainDto> GetCurrentPriceAsync(string symbol)
    {
        try
        {
            var key = GetSymbolPriceKey(symbol);
            var priceString = await _distributedCache.GetAsync(key);

            decimal price;
            if (!decimal.TryParse(priceString, out price))
            {
                _logger.LogError("An error occurred while retrieving the token price, {0}-{1}", symbol, priceString);
                throw new UserFriendlyException("An error occurred while retrieving the token price.");
            }

            return new TokenPriceGrainDto
            {
                Symbol = symbol,
                PriceInUsd = price
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while retrieving the token price, {0}", symbol);
            throw;
        }
    }

    public async Task<TokenPriceGrainDto> GetHistoryPriceAsync(string symbol, string dateTime)
    {
        var key = GetSymbolPriceKey(symbol, dateTime);
        try
        {
            var priceString = await _distributedCache.GetAsync(key);
            decimal price = 0;
            if (priceString.IsNullOrWhiteSpace() || !decimal.TryParse(priceString, out price))
            {
                price = await RefreshHistoryPriceAsync(key, symbol, dateTime);
            }

            return new TokenPriceGrainDto
            {
                Symbol = symbol,
                PriceInUsd = price
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get history price error, {0},{1},{2}", key, symbol, dateTime);
            throw;
        }
    }

    public async Task RefreshCurrentPriceAsync(string symbol = default)
    {
        if (_tokenPriceProviders == null)
        {
            _logger.LogError("token price providers is null.");
            return;
        }

        var symbols = symbol != null ? new[] { symbol } : _tokenPriceWorkerOption.CurrentValue.Symbols.ToArray();
        foreach (var tokenPriceProvider in _tokenPriceProviders)
        {
            try
            {
                var prices = await tokenPriceProvider.GetPriceAsync(symbols);
                if (prices.IsNullOrEmpty())
                {
                    continue;
                }

                foreach (var price in prices)
                {
                    var key = GetSymbolPriceKey(price.Key);
                    var value = price.Value.ToString(CultureInfo.InvariantCulture);
                    await _distributedCache.SetAsync(key, value, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = CommonConstant.DefaultAbsoluteExpiration
                    });
                    _logger.Debug("refresh current price success:{0}-{1}", key, value);
                }

                _logger.LogInformation("refresh current price success, the provider used is: {0}",
                    tokenPriceProvider.GetType().ToString());
                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "get token price error. {0}", tokenPriceProvider.GetType().ToString());
            }
        }
    }

    private async Task<decimal> RefreshHistoryPriceAsync(string key, string symbol, string dateTime)
    {
        decimal price = 0;
        await _lock.WaitAsync();
        try
        {
            var priceString = await _distributedCache.GetAsync(key);
            if (priceString.IsNullOrWhiteSpace() || !decimal.TryParse(priceString, out price))
            {
                _logger.LogInformation("refresh history price start... {0}", key);
                foreach (var tokenPriceProvider in _tokenPriceProviders)
                {
                    try
                    {
                        var historyPrice = await tokenPriceProvider.GetHistoryPriceAsync(symbol, dateTime);
                        if (historyPrice == 0)
                        {
                            continue;
                        }

                        price = historyPrice;
                        await _distributedCache.SetAsync(key, price.ToString(CultureInfo.CurrentCulture),
                            new DistributedCacheEntryOptions
                            {
                                AbsoluteExpiration = CommonConstant.DefaultAbsoluteExpiration
                            });
                        _logger.LogInformation("refresh history price success...{key}", key);
                        break;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "refresh history price error, {0},{1},{2},{3}", key, symbol, dateTime,
                            tokenPriceProvider.GetType().ToString());
                    }
                }
            }
        }
        finally
        {
            _lock.Release();
        }

        return price;
    }

    private string GetSymbolPriceKey(string symbol)
    {
        return
            $"{_tokenPriceWorkerOption.CurrentValue.Prefix}:{_tokenPriceWorkerOption.CurrentValue.PricePrefix}:{symbol?.ToUpper()}";
    }

    private string GetSymbolPriceKey(string symbol, string dateTime)
    {
        return
            $"{_tokenPriceWorkerOption.CurrentValue.Prefix}:{_tokenPriceWorkerOption.CurrentValue.PricePrefix}:{symbol?.ToUpper()}:{dateTime}";
    }
}