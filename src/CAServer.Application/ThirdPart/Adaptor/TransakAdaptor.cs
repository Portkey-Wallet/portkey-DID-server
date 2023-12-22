using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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
using Volo.Abp;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Adaptor;

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

    public async Task PreHeatCaches()
    {
        if (_thirdPartOptions?.CurrentValue?.Transak == null) return;

        _logger.LogInformation("Transak adaptor pre heat start");
        var cryptoTask = GetTransakCryptoListWithCache();
        var fiatTask = GetTransakFiatListWithCache(OrderTransDirect.BUY.ToString());
        var countryTask = GetTransakCountryWithCache();
        await Task.WhenAll(cryptoTask, fiatTask, countryTask);
        _logger.LogInformation("Transak adaptor pre heat done");
    }

    // cached crypto list
    private async Task<List<TransakCryptoItem>> GetTransakCryptoListWithCache()
    {
        return await _cryptoCache.GetOrAddAsync(CryptoCacheKey,
            async () => await _transakProvider.GetCryptoCurrenciesAsync(),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration =
                    DateTimeOffset.Now.AddMinutes(_thirdPartOptions.CurrentValue.Transak.CryptoListExpirationMinutes)
            });
    }

    private async Task<List<TransakFiatItem>> GetTransakFiatCurrencies()
    {
        _logger.LogDebug("Transak fiat query start");
        var fiatList = await _transakProvider.GetFiatCurrenciesAsync();

        _logger.LogDebug("Transak fiat query finish");
        await _transakProvider.SetSvgUrl(fiatList);

        _logger.LogDebug("Transak fiat upload finish");
        return fiatList;
    }

    // cached fiat list
    private async Task<List<TransakFiatItem>> GetTransakFiatListWithCache(string type,
        string crypto = null)
    {
        var fiatList = await _fiatCache.GetOrAddAsync(FiatCacheKey,
            async () => await GetTransakFiatCurrencies(),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration =
                    DateTimeOffset.Now.AddMinutes(_thirdPartOptions.CurrentValue.Transak.FiatListExpirationMinutes)
            });

        // filter input crypto
        var notSupportedFiat = new ConcurrentDictionary<string, List<string>>();
        if (crypto.NotNullOrEmpty())
        {
            var cryptoList = await GetTransakCryptoListWithCache();
            var theCrypto = cryptoList
                .FirstOrDefault(c => c.Symbol == crypto);
            if (theCrypto != null)
            {
                foreach (var transakCryptoFiatNotSupported in theCrypto.Network.FiatCurrenciesNotSupported)
                {
                    notSupportedFiat
                        .GetOrAdd(transakCryptoFiatNotSupported.FiatCurrency, k => new List<string>())
                        .Add(transakCryptoFiatNotSupported.PaymentMethod);
                }
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
    private async Task<Dictionary<string, TransakCountry>> GetTransakCountryWithCache()
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
    private async Task<TransakRampPrice> GetTransakPriceWithCache(string paymentMethod,
        RampDetailRequest rampPriceRequest)
    {
        rampPriceRequest.Network =
            _rampOptions.CurrentValue.Providers["Transak"].NetworkMapping[rampPriceRequest.Network];
        var transakPriceRequest = _objectMapper.Map<RampDetailRequest, GetRampPriceRequest>(rampPriceRequest);
        transakPriceRequest.PaymentMethod = paymentMethod;


        Func<RampDetailRequest, string> priceCacheKey = req => "Ramp:transak:price:" + string.Join(
            CommonConstant.Underline, req.Crypto,
            req.Network, req.CryptoAmount ?? 0, req.Fiat,
            req.Country, req.FiatAmount ?? 0);

        DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration =
                DateTimeOffset.Now.AddMinutes(_thirdPartOptions.CurrentValue.Transak.OrderQuoteExpirationMinutes)
        };
        Func<DistributedCacheEntryOptions> optionsFunc = () => options;

        return await _rampPrice.GetOrAddAsync(priceCacheKey(rampPriceRequest),
            async () => await _transakProvider.GetRampPriceAsync(transakPriceRequest),
            optionsFunc);
    }


    public async Task<List<RampFiatItem>> GetFiatListAsync(RampFiatRequest rampFiatRequest)
    {
        try
        {
            AssertHelper.IsTrue(rampFiatRequest.Crypto.IsNullOrEmpty() || rampFiatRequest.Network.NotNullOrEmpty(),
                "Network required when Crypto exists.");


            // query fiat ASYNC
            var fiatTask = GetTransakFiatListWithCache(rampFiatRequest.Type, rampFiatRequest.Crypto);

            // query country ASYNC
            var countryTask = GetTransakCountryWithCache();


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

    private async Task<TransakFiatItem> GetTransakFiatItem(string type, string fiat, [CanBeNull] string country = null,
        [CanBeNull] string crypto = null)
    {
        // find fiat info 
        var fiatList = await GetTransakFiatListWithCache(type, crypto);
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
            var fiat = await GetTransakFiatItem(rampLimitRequest.Type, rampLimitRequest.Fiat, rampLimitRequest.Country,
                rampLimitRequest.Crypto);

            // find any payment option
            var paymentOption = fiat.MaxLimitPayment(rampLimitRequest.Type);

            AssertHelper.NotNull(paymentOption, "Fiat {Fiat} paymentOption missing", rampLimitRequest.Fiat);

            var rampDetailRequest = _objectMapper.Map<RampLimitRequest, RampDetailRequest>(rampLimitRequest);

            if (rampDetailRequest.IsBuy())
            {
                rampDetailRequest.FiatAmount = paymentOption.MinAmount;
            }
            else
            {
                rampDetailRequest.FiatAmount = paymentOption.MinAmountForPayOut;
            }


            var priceInfo = await GetTransakPriceWithCache(paymentOption.Id, rampDetailRequest);


            var fiatLimit = new CurrencyLimit(fiat.Symbol, "", "");
            var cryptoLimit = new CurrencyLimit(rampLimitRequest.Crypto, "", "");

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
            var rampDetailRequest = _objectMapper.Map<RampExchangeRequest, RampDetailRequest>(rampExchangeRequest);

            rampDetailRequest.Network =
                _rampOptions.CurrentValue.Providers["Transak"].NetworkMapping[rampDetailRequest.Network];
            var cryptoList = await GetTransakCryptoListWithCache();
            var theCrypto = cryptoList
                .Where(c => c.Network.Name == rampDetailRequest.Network)
                .FirstOrDefault(c => c.Symbol == rampDetailRequest.Crypto);
            if (theCrypto == null)
            {
                _logger.LogWarning($"Crypto is invalid,param:{rampDetailRequest}");
                return null;
            }

            var notSupportedFiat = new ConcurrentDictionary<string, List<string>>();
            foreach (var transakCryptoFiatNotSupported in theCrypto.Network.FiatCurrenciesNotSupported)
            {
                notSupportedFiat
                    .GetOrAdd(transakCryptoFiatNotSupported.FiatCurrency, k => new List<string>())
                    .Add(transakCryptoFiatNotSupported.PaymentMethod);
            }


            var fiatList = await _fiatCache.GetOrAddAsync(FiatCacheKey,
                async () => await GetTransakFiatCurrencies(),
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
                _logger.LogWarning($"paymentOption is no find,param:{rampDetailRequest}");
                return null;
            }


            decimal minFiatAmount = 0;
            rampDetailRequest.Crypto = CryptoUSDT;
            rampDetailRequest.Network = CryptoNetwork;
            rampDetailRequest.Country = null;
            if (rampDetailRequest.IsBuy())
            {
                rampDetailRequest.CryptoAmount = paymentOption.MinAmount;

                var limiPrice = await GetTransakPriceWithCache(paymentOption.Id, rampDetailRequest);

                rampDetailRequest.Crypto = rampDetailRequest.Crypto;
                rampDetailRequest.FiatAmount = limiPrice.FiatAmount;
                rampDetailRequest.Network = rampDetailRequest.Network;
                rampDetailRequest.CryptoAmount = null;
            }
            else if (rampDetailRequest.IsSell())
            {
                if (!paymentOption.IsPayOutAllowed)
                {
                    _logger.LogWarning($"fiat is no support payout,{rampDetailRequest}");
                    return null;
                }

                rampDetailRequest.CryptoAmount = paymentOption.MinAmountForPayOut;
                var limiPrice = await GetTransakPriceWithCache(paymentOption.Id, rampDetailRequest);

                rampDetailRequest.Crypto = rampDetailRequest.Crypto;
                rampDetailRequest.FiatAmount = limiPrice.FiatAmount;
                rampDetailRequest.Network = rampDetailRequest.Network;
                rampDetailRequest.CryptoAmount = null;
            }

            var limitCurrencyPrice = await GetTransakPriceWithCache(paymentOption.Id, rampDetailRequest);
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

    public async Task<RampPriceDto> GetRampPriceAsync(RampDetailRequest rampDetailRequest)
    {
        try
        {
            var fiat = await GetTransakFiatItem(rampDetailRequest.Type, rampDetailRequest.Fiat,
                rampDetailRequest.Country, rampDetailRequest.Crypto);

            var paymentOption = fiat.MaxLimitPayment(rampDetailRequest.Type);

            AssertHelper.NotNull(paymentOption, "Fiat {Fiat} paymentOption missing", rampDetailRequest.Fiat);

            var commonPrice = await GetTransakPriceWithCache(paymentOption.Id, rampDetailRequest);


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
            var fiatItem =
                await GetTransakFiatItem(rampDetailRequest.Type, rampDetailRequest.Fiat, rampDetailRequest.Country,
                    rampDetailRequest.Crypto);
            var payment = fiatItem.MaxLimitPayment(rampDetailRequest.Type);
            AssertHelper.NotNull(payment, "Payment of fiatItem not found, type={}, fiat={Fiat}, country={Country}",
                rampDetailRequest.Type, rampDetailRequest.Fiat, rampDetailRequest.Country);

            var price = await GetTransakPriceWithCache(payment.Id, rampDetailRequest);

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