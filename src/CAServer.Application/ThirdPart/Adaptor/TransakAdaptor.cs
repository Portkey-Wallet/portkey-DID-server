using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Cache;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart.Dtos.Ramp;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Transak;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Adaptor;

[DisableAuditing]
public class TransakAdaptor : IThirdPartAdaptor, ISingletonDependency
{
    private const string CryptoCacheKey = "Ramp:transak:crypto";
    private const string FiatCacheKey = "Ramp:transak:fiat";
    private const string CountryCacheKey = "Ramp:transak:country";
    private const string CryptoUSDT = "USDT";
    private const string CryptoNetwork = "ethereum";
    private const decimal DefaultFiatAccount = 200;

    private readonly TransakProvider _transakProvider;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly IOptionsMonitor<RampOptions> _rampOptions;
    private readonly ILocalMemoryCache<List<TransakCryptoItem>> _cryptoCache;
    private readonly ILocalMemoryCache<List<TransakFiatItem>> _fiatCache;
    private readonly ILocalMemoryCache<Dictionary<string, TransakCountry>> _countryCache;
    private readonly IDistributedCache<TransakRampPrice> _rampPrice;
    private readonly ILogger<TransakAdaptor> _logger;
    private readonly IObjectMapper _objectMapper;

    public TransakAdaptor(TransakProvider transakProvider, IOptionsMonitor<ThirdPartOptions> thirdPartOptions,
        ILocalMemoryCache<List<TransakCryptoItem>> cryptoCache, ILocalMemoryCache<List<TransakFiatItem>> fiatCache,
        ILocalMemoryCache<Dictionary<string, TransakCountry>> countryCache,
        IDistributedCache<TransakRampPrice> rampPrice, ILogger<TransakAdaptor> logger, IObjectMapper objectMapper,
        IOptionsMonitor<RampOptions> rampOptions)
    {
        _transakProvider = transakProvider;
        _thirdPartOptions = thirdPartOptions;
        _cryptoCache = cryptoCache;
        _fiatCache = fiatCache;
        _countryCache = countryCache;
        _rampPrice = rampPrice;
        _logger = logger;
        _objectMapper = objectMapper;
        _rampOptions = rampOptions;
    }


    public string ThirdPart()
    {
        return ThirdPartNameType.Transak.ToString();
    }

    public string MappingToTransakNetwork(string network)
    {
        if (network.IsNullOrEmpty()) return network;
        var mappingExists = _rampOptions.CurrentValue.Provider(ThirdPartNameType.Transak).NetworkMapping
            .TryGetValue(network, out var mappingNetwork);
        return mappingExists ? mappingNetwork : network;
    }

    public string MappingFromTransakNetwork(string network)
    {
        if (network.IsNullOrEmpty()) return network;
        var mappingNetwork = _rampOptions.CurrentValue.Provider(ThirdPartNameType.Transak).NetworkMapping
            .FirstOrDefault(kv => kv.Value == network);
        return mappingNetwork.Key.DefaultIfEmpty(network);
    }

    public string MappingToTransakSymbol(string symbol)
    {
        if (symbol.IsNullOrEmpty()) return symbol;
        var mappingExists = _rampOptions.CurrentValue.Provider(ThirdPartNameType.Transak).SymbolMapping
            .TryGetValue(symbol, out var mappingSymbol);
        return mappingExists ? mappingSymbol : symbol;
    }

    public string MappingFromTransakSymbol(string symbol)
    {
        if (symbol.IsNullOrEmpty()) return symbol;
        var mappingNetwork = _rampOptions.CurrentValue.Provider(ThirdPartNameType.Transak).SymbolMapping
            .FirstOrDefault(kv => kv.Value == symbol);
        return mappingNetwork.Key.DefaultIfEmpty(symbol);
    }


    public async Task<List<RampCurrencyItem>> GetCryptoListAsync(RampCryptoRequest request)
    {
        try
        {
            var cryptoList =
                await GetTransakCryptoListWithCacheAsync(request.Type, MappingToTransakNetwork(request.Network), null,
                    request.Fiat);
            _logger.LogInformation("GetCryptoListAsync {0} request:{1} response:{2}", ThirdPart(),
                JsonConvert.SerializeObject(request), JsonConvert.SerializeObject(cryptoList));
            AssertHelper.NotEmpty(cryptoList, "Crypto list empty");

            var transakNetwork = MappingToTransakNetwork(request.Network);
            return cryptoList.Select(item => new RampCurrencyItem()
            {
                Symbol = MappingFromTransakSymbol(item.Symbol),
                Network = MappingFromTransakNetwork(item.Network?.Name ?? transakNetwork)
            }).ToList();
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(e, "{ThirdPart} GetCryptoListAsync failed", ThirdPart());
        }
        catch (Exception e)
        {
            //报错日志打出
            _logger.LogError(e, "{ThirdPart} GetCryptoListAsync ERROR", ThirdPart());
        }

        return new List<RampCurrencyItem>();
    }

    public async Task PreHeatCachesAsync()
    {
        if (_thirdPartOptions?.CurrentValue?.Transak == null ||
            (_thirdPartOptions?.CurrentValue?.Transak.AppId.IsNullOrEmpty() ?? true)) return;

        _logger.LogInformation("Transak adaptor pre heat start");
        var cryptoTask = GetTransakCryptoListWithCacheAsync(OrderTransDirect.BUY.ToString());
        var fiatTask = GetTransakFiatListWithCacheAsync(OrderTransDirect.BUY.ToString());
        var countryTask = GetTransakCountryWithCacheAsync();
        await Task.WhenAll(cryptoTask, fiatTask, countryTask);
        _logger.LogInformation("Transak adaptor pre heat done");
    }

    public async Task<List<TransakCryptoItem>> GetCryptoCurrenciesAsync()
    {
        return await _transakProvider.GetCryptoCurrenciesAsync();
    }
    
    // cached crypto list
    private async Task<List<TransakCryptoItem>> GetTransakCryptoListWithCacheAsync(string type,
        [CanBeNull] string network = null, [CanBeNull] string crypto = null, [CanBeNull] string fiat = null)
    {
        var cachedCryptoList = await _cryptoCache.GetOrAddAsync(CryptoCacheKey,
            async () => await _transakProvider.GetCryptoCurrenciesAsync(),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration =
                    DateTimeOffset.Now.AddMinutes(_thirdPartOptions.CurrentValue.Transak.CryptoListExpirationMinutes)
            });

        var fallBackNotSupportedList = new List<TransakCryptoFiatNotSupported>();
        return cachedCryptoList
            .Where(item => item.IsAllowed)
            .Where(item => item.IsPayInAllowed || type == OrderTransDirect.BUY.ToString())
            .Where(item => crypto.IsNullOrEmpty() || item.Symbol == crypto)
            .Where(item => network.IsNullOrEmpty() || item.Network?.Name == network)
            .Where(item =>
                fiat.IsNullOrEmpty() ||
                (item.Network?.FiatCurrenciesNotSupported ?? fallBackNotSupportedList).All(c => c.FiatCurrency != fiat))
            .GroupBy(item => string.Join(CommonConstant.Underline, item.Symbol,
                item.Network?.Name ?? CommonConstant.EmptyString))
            .Select(g => g.First())
            .ToList();
    }

    private async Task<List<TransakFiatItem>> GetTransakFiatCurrenciesAsync()
    {
        _logger.LogDebug("Transak fiat query start");
        var fiatList = await _transakProvider.GetFiatCurrenciesAsync();

        _logger.LogDebug("Transak fiat query finish");
        await _transakProvider.SetSvgUrlAsync(fiatList);

        _logger.LogDebug("Transak fiat upload finish");
        return fiatList;
    }

    // cached fiat list
    private async Task<List<TransakFiatItem>> GetTransakFiatListWithCacheAsync(string type,
        string crypto = null)
    {
        var mappingCrypto = MappingToTransakSymbol(crypto);
        var mappingNetwork = MappingToTransakNetwork(CommonConstant.MainChainId);
        var fiatList = await _fiatCache.GetOrAddAsync(FiatCacheKey,
            async () => await GetTransakFiatCurrenciesAsync(),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration =
                    DateTimeOffset.Now.AddMinutes(_thirdPartOptions.CurrentValue.Transak.FiatListExpirationMinutes)
            });

        // filter input crypto
        var notSupportedFiat = new ConcurrentDictionary<string, List<string>>();
        if (mappingCrypto.NotNullOrEmpty())
        {
            var cryptoList = await GetTransakCryptoListWithCacheAsync(type, crypto: mappingCrypto);
            var theCrypto = cryptoList
                .Where(c => c.Network?.Name == mappingNetwork)
                .FirstOrDefault(c => c.Symbol == mappingCrypto);
            if (theCrypto == null) return new List<TransakFiatItem>();

            foreach (var transakCryptoFiatNotSupported in theCrypto.Network.FiatCurrenciesNotSupported)
            {
                notSupportedFiat
                    .GetOrAdd(transakCryptoFiatNotSupported.FiatCurrency, k => new List<string>())
                    .Add(transakCryptoFiatNotSupported.PaymentMethod);
            }
        }

        // filter by type
        return fiatList.Where(fiat => fiat.PaymentOptions
            // filter not support payment
            .Where(p => !notSupportedFiat.TryGetValue(fiat.Symbol, out var payment) || !payment.Contains(p.Id))
            // filter not support sell
            .Where(payment => type == OrderTransDirect.BUY.ToString() || payment.IsPayOutAllowed)
            .Any(payment => payment.IsActive)
        ).ToList();
    }

    // cached country list
    private async Task<Dictionary<string, TransakCountry>> GetTransakCountryWithCacheAsync()
    {
        return await _countryCache.GetOrAddAsync(CountryCacheKey,
            async () => (await _transakProvider.GetTransakCountriesAsync()).ToDictionary(c => c.Alpha2, c => c),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration =
                    DateTimeOffset.Now.AddMinutes(_thirdPartOptions.CurrentValue.Transak.FiatListExpirationMinutes)
            });
    }

    // cached ramp price
    private async Task<TransakRampPrice> GetTransakPriceWithCacheAsync(string paymentMethod,
        RampDetailRequest rampPriceRequest)
    {
        var transakPriceRequest = _objectMapper.Map<RampDetailRequest, GetRampPriceRequest>(rampPriceRequest);
        transakPriceRequest.PaymentMethod = paymentMethod;
        transakPriceRequest.Network = MappingToTransakNetwork(transakPriceRequest.Network);
        transakPriceRequest.CryptoCurrency = MappingToTransakSymbol(transakPriceRequest.CryptoCurrency);

        var crypto =
            await GetTransakCryptoListWithCacheAsync(rampPriceRequest.Type, transakPriceRequest.Network,
                transakPriceRequest.CryptoCurrency);
        AssertHelper.NotEmpty(crypto, "Crypto not support, type={}, network={}, crypto={}", rampPriceRequest.Type,
            transakPriceRequest.Network, transakPriceRequest.CryptoCurrency);

        Func<RampDetailRequest, string> priceCacheKey = req => "Ramp:transak:price:" + string.Join(
            CommonConstant.Underline, req.Crypto,
            req.Network, req.CryptoAmount ?? 0, req.Fiat,
            req.Country, req.FiatAmount ?? 0);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration =
                DateTimeOffset.Now.AddMinutes(_thirdPartOptions.CurrentValue.Transak.OrderQuoteExpirationMinutes)
        };

        return await _rampPrice.GetOrAddAsync(priceCacheKey(rampPriceRequest),
            async () => await _transakProvider.GetRampPriceAsync(transakPriceRequest),
            () => options);
    }


    public async Task<List<RampFiatItem>> GetFiatListAsync(RampFiatRequest rampFiatRequest)
    {
        try
        {
            AssertHelper.IsTrue(rampFiatRequest.Crypto.IsNullOrEmpty() || rampFiatRequest.Network.NotNullOrEmpty(),
                "Network required when Crypto exists.");


            // query fiat ASYNC
            var fiatTask = GetTransakFiatListWithCacheAsync(rampFiatRequest.Type, rampFiatRequest.Crypto);

            // query country ASYNC
            var countryTask = GetTransakCountryWithCacheAsync();


            // wait fiatTask and countryTask
            await Task.WhenAll(fiatTask, countryTask);

            var countryDict = countryTask.Result;
            var fiatList = fiatTask.Result
                .SelectMany(f =>
                    f.SupportingCountries.Select(country => new RampFiatItem
                    {
                        Country = country,
                        Symbol = f.Symbol,
                        CountryName = countryDict.GetValueOrDefault(country)?.Name ?? country,
                        Icon = f.IconUrl
                    }))
                .ToList();
            return fiatList;
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(e, "{ThirdPart} GetFiatListAsync failed", ThirdPart());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{ThirdPart} GetFiatListAsync error", ThirdPart());
        }

        return new List<RampFiatItem>();
    }

    private async Task<TransakFiatItem> GetTransakFiatItemAsync(string type, string fiat,
        [CanBeNull] string country = null,
        [CanBeNull] string crypto = null)
    {
        // find fiat info 
        var fiatList = await GetTransakFiatListWithCacheAsync(type, crypto);
        AssertHelper.NotEmpty(fiatList, "Transak fiat list empty");

        var fiatItem = fiatList
            .Where(f => f.Symbol == fiat)
            .Where(f => !f.SupportingCountries.IsNullOrEmpty())
            .FirstOrDefault(f => country.IsNullOrEmpty() || f.SupportingCountries.Contains(country));
        AssertHelper.NotNull(fiatItem, "Fiat {Fiat} not found", fiat);
        AssertHelper.NotEmpty(fiatItem.PaymentOptions, "Fiat {Fiat} payment empty", fiat);
        return fiatItem;
    }

    public async Task<RampLimitDto> GetRampLimitAsync(RampLimitRequest rampLimitRequest)
    {
        try
        {
            // find fiat info 
            var fiat = await GetTransakFiatItemAsync(rampLimitRequest.Type, rampLimitRequest.Fiat,
                rampLimitRequest.Country,
                rampLimitRequest.Crypto);

            // find any payment option
            var paymentOption = fiat.MaxLimitPayment(rampLimitRequest.Type);
            AssertHelper.NotNull(paymentOption, "Fiat {Fiat} paymentOption missing", rampLimitRequest.Fiat);

            var rampDetailRequest = _objectMapper.Map<RampLimitRequest, RampDetailRequest>(rampLimitRequest);
            rampDetailRequest.Crypto = MappingToTransakSymbol(rampDetailRequest.Crypto);
            rampDetailRequest.Network = MappingToTransakNetwork(rampDetailRequest.Network);
            rampDetailRequest.FiatAmount =
                rampDetailRequest.IsBuy() ? paymentOption.MinAmount : paymentOption.MinAmountForPayOut;

            var priceInfo = await GetTransakPriceWithCacheAsync(paymentOption.Id, rampDetailRequest);


            var fiatLimit = new CurrencyLimit(fiat.Symbol, "", "");
            var cryptoLimit = new CurrencyLimit(rampDetailRequest.Crypto, "", "");

            if (rampDetailRequest.IsBuy())
            {
                fiatLimit.MinLimit = paymentOption.MinAmount.ToString();
                fiatLimit.MaxLimit = paymentOption.MaxAmount.ToString();

                cryptoLimit.MinLimit = (paymentOption.MinAmount * priceInfo.ConversionPrice).ToString();
                cryptoLimit.MaxLimit = (paymentOption.MaxAmount * priceInfo.ConversionPrice).ToString();
            }
            else
            {
                fiatLimit.MinLimit = paymentOption.MinAmountForPayOut.ToString();
                fiatLimit.MaxLimit = paymentOption.MaxAmountForPayOut.ToString();

                cryptoLimit.MinLimit = (paymentOption.MinAmountForPayOut * priceInfo.ConversionPrice).ToString();
                cryptoLimit.MaxLimit = (paymentOption.MaxAmountForPayOut * priceInfo.ConversionPrice).ToString();
            }

            return new RampLimitDto
            {
                Fiat = fiatLimit,
                Crypto = cryptoLimit
            };
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(e, "{ThirdPart} GetRampLimitAsync failed", ThirdPart());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{ThirdPart} GetRampLimitAsync error", ThirdPart());
        }

        return null;
    }

    public async Task<decimal?> GetRampExchangeAsync(RampExchangeRequest rampExchangeRequest)
    {
        try
        {
            // copy request
            var rampDetailRequest = _objectMapper.Map<RampExchangeRequest, RampDetailRequest>(rampExchangeRequest);
            rampDetailRequest.Network = MappingToTransakNetwork(rampDetailRequest.Network);
            rampDetailRequest.Crypto = MappingToTransakSymbol(rampDetailRequest.Crypto);
            var cryptoList = await GetTransakCryptoListWithCacheAsync(rampDetailRequest.Type, rampDetailRequest.Network,
                rampDetailRequest.Crypto);
            var theCrypto = cryptoList.FirstOrDefault(c => c.Symbol == rampDetailRequest.Crypto);
            AssertHelper.NotNull(theCrypto, "Crypto not support: type={Type}, network={Net}, crypto={Crypto}",
                rampExchangeRequest.Type, rampExchangeRequest.Network, rampExchangeRequest.Crypto);

            var notSupportedFiat = new ConcurrentDictionary<string, List<string>>();
            foreach (var transakCryptoFiatNotSupported in theCrypto.Network.FiatCurrenciesNotSupported)
            {
                notSupportedFiat
                    .GetOrAdd(transakCryptoFiatNotSupported.FiatCurrency, k => new List<string>())
                    .Add(transakCryptoFiatNotSupported.PaymentMethod);
            }

            var fiatList = await _fiatCache.GetOrAddAsync(FiatCacheKey,
                async () => await GetTransakFiatCurrenciesAsync(),
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration =
                        DateTimeOffset.Now.AddMinutes(10)
                });


            var fiat = fiatList.Where(a => a.Symbol == rampDetailRequest.Fiat)
                .Where(f => !f.SupportingCountries.IsNullOrEmpty())
                .FirstOrDefault(f => f.SupportingCountries.Contains(rampDetailRequest.Country));

            if (fiat == null)
            {
                _logger.LogWarning($"fiat is no find,param:{rampDetailRequest}");
                return null;
            }

            var paymentOption = fiat.PaymentOptions.Where(p =>
                    !notSupportedFiat.TryGetValue(fiat.Symbol, out var payment) || !payment.Contains(p.Id))
                .FirstOrDefault(payment => payment.IsActive);

            if (paymentOption == null)
            {
                _logger.LogWarning("paymentOption is no find,param:{RampDetailRequest}", rampDetailRequest);
                return null;
            }


            decimal minFiatAmount = 0;
            rampDetailRequest.Crypto = CryptoUSDT;
            rampDetailRequest.Network = CryptoNetwork;
            rampDetailRequest.Country = null;
            if (rampDetailRequest.IsBuy())
            {
                rampDetailRequest.CryptoAmount = paymentOption.MinAmount;

                var limitPrice = await GetTransakPriceWithCacheAsync(paymentOption.Id, rampDetailRequest);

                rampDetailRequest.Crypto = rampDetailRequest.Crypto;
                rampDetailRequest.FiatAmount = limitPrice.FiatAmount;
                rampDetailRequest.Network = rampDetailRequest.Network;
                rampDetailRequest.CryptoAmount = null;
            }
            else if (rampDetailRequest.IsSell())
            {
                if (!paymentOption.IsPayOutAllowed)
                {
                    _logger.LogWarning("fiat is no support payout,{RampDetailRequest}", rampDetailRequest.Fiat);
                    return null;
                }

                rampDetailRequest.CryptoAmount = paymentOption.MinAmountForPayOut;
                var limitPrice = await GetTransakPriceWithCacheAsync(paymentOption.Id, rampDetailRequest);

                rampDetailRequest.Crypto = rampDetailRequest.Crypto;
                rampDetailRequest.FiatAmount = limitPrice.FiatAmount;
                rampDetailRequest.Network = rampDetailRequest.Network;
                rampDetailRequest.CryptoAmount = null;
            }

            var limitCurrencyPrice = await GetTransakPriceWithCacheAsync(paymentOption.Id, rampDetailRequest);
            return limitCurrencyPrice.FiatCryptoExchange();
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(e, "{ThirdPart} GetRampExchangeAsync failed", ThirdPart());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{ThirdPart} GetRampExchangeAsync error", ThirdPart());
        }

        return null;
    }

    private async Task<bool> InRampLimit(RampDetailRequest rampDetailRequest)
    {
        var rampLimitRequest = _objectMapper.Map<RampDetailRequest, RampLimitRequest>(rampDetailRequest);
        var limit = await GetRampLimitAsync(rampLimitRequest);
        if (limit == null) return false;

        var currencyLimit = rampDetailRequest.IsBuy() ? limit.Fiat : limit.Crypto;
        var amount = (rampDetailRequest.IsBuy() ? rampDetailRequest.FiatAmount : rampDetailRequest.CryptoAmount) ?? 0;
        return amount >= currencyLimit.MinLimit.SafeToDecimal() && amount <= currencyLimit.MaxLimit.SafeToDecimal();
    }

    public async Task<RampPriceDto> GetRampPriceAsync(RampDetailRequest rampDetailRequest)
    {
        try
        {
            if (!await InRampLimit(rampDetailRequest)) return null;

            var transakNetwork = MappingToTransakNetwork(rampDetailRequest.Network);
            var transakCrypto = MappingToTransakSymbol(rampDetailRequest.Crypto);
            var fiat = await GetTransakFiatItemAsync(rampDetailRequest.Type, rampDetailRequest.Fiat,
                rampDetailRequest.Country, MappingToTransakSymbol(rampDetailRequest.Crypto));
            var paymentOption = fiat.MaxLimitPayment(rampDetailRequest.Type);
            AssertHelper.NotNull(paymentOption, "Fiat {Fiat} paymentOption missing", rampDetailRequest.Fiat);

            var commonPrice = await GetTransakPriceWithCacheAsync(paymentOption.Id, rampDetailRequest);
            var transakFeePercent = commonPrice.TransakFeePercent();
            var fiatAmount = rampDetailRequest.IsBuy()
                ? rampDetailRequest.FiatAmount ?? 0
                : rampDetailRequest.CryptoAmount / commonPrice.ConversionPrice ?? 0;

            var networkFee = commonPrice.NetworkFee();
            var transakFee = fiatAmount * transakFeePercent;
            fiatAmount = fiatAmount - transakFee - networkFee;

            var cryptoAmount = rampDetailRequest.IsBuy()
                ? fiatAmount * commonPrice.ConversionPrice
                : rampDetailRequest.CryptoAmount ?? 0;

            var rampPrice = _objectMapper.Map<TransakRampPrice, RampPriceDto>(commonPrice);
            rampPrice.ThirdPart = ThirdPart();
            rampPrice.FiatAmount = fiatAmount.ToString(CultureInfo.InvariantCulture);
            rampPrice.CryptoAmount = cryptoAmount.ToString(8, DecimalHelper.RoundingOption.Floor);
            rampPrice.FeeInfo = new RampFeeInfo
            {
                NetworkFee = FeeItem.Fiat(rampDetailRequest.Fiat,
                    networkFee.ToString(CultureInfo.InvariantCulture)),
                RampFee = FeeItem.Fiat(rampDetailRequest.Fiat,
                    transakFee.ToString(CultureInfo.InvariantCulture)),
            };
            return rampPrice;
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(e, "{ThirdPart} GetRampExchangeAsync failed", ThirdPart());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{ThirdPart} GetRampExchangeAsync error", ThirdPart());
        }

        return null;
    }

    public async Task<ProviderRampDetailDto> GetRampDetailAsync(RampDetailRequest rampDetailRequest)
    {
        try
        {
            if (!await InRampLimit(rampDetailRequest)) return null;

            var fiatItem =
                await GetTransakFiatItemAsync(rampDetailRequest.Type, rampDetailRequest.Fiat, rampDetailRequest.Country,
                    rampDetailRequest.Crypto);
            var payment = fiatItem.MaxLimitPayment(rampDetailRequest.Type);
            AssertHelper.NotNull(payment, "Payment of fiatItem not found, type={}, fiat={Fiat}, country={Country}",
                rampDetailRequest.Type, rampDetailRequest.Fiat, rampDetailRequest.Country);

            var price = await GetTransakPriceWithCacheAsync(payment.Id, rampDetailRequest);

            var providerRampDetail = _objectMapper.Map<TransakRampPrice, ProviderRampDetailDto>(price);
            providerRampDetail.ThirdPart = ThirdPart();
            providerRampDetail.FeeInfo = new RampFeeInfo
            {
                NetworkFee = FeeItem.Fiat(rampDetailRequest.Fiat,
                    price.NetworkFee().ToString(CultureInfo.InvariantCulture)),
                RampFee = FeeItem.Fiat(rampDetailRequest.Fiat,
                    price.TransakFee().ToString(CultureInfo.InvariantCulture)),
            };
            return providerRampDetail;
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(e, "{ThirdPart} GetRampDetailAsync failed", ThirdPart());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{ThirdPart} GetRampDetailAsync error", ThirdPart());
        }

        return null;
    }
}