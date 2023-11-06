using System;
using System.Collections.Generic;
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
using Volo.Abp.Caching;

namespace CAServer.ThirdPart.Adaptor;

public class TransakAdaptor : CAServerAppService, IThirdPartAdaptor
{
    private const string CryptoCacheKey = "Ramp:transak:crypto";
    private const string FiatCacheKey = "Ramp:transak:fiat";
    private const string CountryCacheKey = "Ramp:transak:country";
    private const decimal DefaultFiatAccount = 200;

    private readonly TransakProvider _transakProvider;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly ILocalMemoryCache<List<TransakCryptoItem>> _cryptoCache;
    private readonly ILocalMemoryCache<List<TransakFiatItem>> _fiatCache;
    private readonly ILocalMemoryCache<Dictionary<string, TransakCountry>> _countryCache;
    private readonly IDistributedCache<TransakRampPrice> _rampPrice;

    public TransakAdaptor(TransakProvider transakProvider, IOptionsMonitor<ThirdPartOptions> thirdPartOptions,
        ILocalMemoryCache<List<TransakCryptoItem>> cryptoCache, ILocalMemoryCache<List<TransakFiatItem>> fiatCache,
        ILocalMemoryCache<Dictionary<string, TransakCountry>> countryCache, IDistributedCache<TransakRampPrice> rampPrice)
    {
        _transakProvider = transakProvider;
        _thirdPartOptions = thirdPartOptions;
        _cryptoCache = cryptoCache;
        _fiatCache = fiatCache;
        _countryCache = countryCache;
        _rampPrice = rampPrice;
    }


    public string ThirdPart()
    {
        return ThirdPartNameType.Transak.ToString();
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

    // cached fiat list
    private async Task<List<TransakFiatItem>> GetTransakFiatListWithCache(string type)
    {
        var fiatList = await _fiatCache.GetOrAddAsync(FiatCacheKey,
            async () => await _transakProvider.GetFiatCurrenciesAsync(), // TODO nzc save svg image
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration =
                    DateTimeOffset.Now.AddMinutes(_thirdPartOptions.CurrentValue.Transak.FiatListExpirationMinutes)
            });
        // filter by type
        return fiatList.Where(fiat => fiat.PaymentOptions
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
        var transakPriceRequest = ObjectMapper.Map<RampDetailRequest, GetRampPriceRequest>(rampPriceRequest);
        transakPriceRequest.PaymentMethod = paymentMethod;
        Func<RampDetailRequest, string> priceCacheKey = req => "Ramp:transak:price:" + string.Join(
            CommonConstant.Underline, req.Crypto,
            req.Network, req.CryptoAmount ?? 0, req.Fiat,
            req.Country, req.FiatAmount ?? 0);
        // query transak price
        var transakPrice = await _rampPrice.GetOrAddAsync(priceCacheKey(rampPriceRequest),
            async () => await _transakProvider.GetRampPriceAsync(transakPriceRequest),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration =
                    DateTimeOffset.Now.AddMinutes(_thirdPartOptions.CurrentValue.Transak.OrderQuoteExpirationMinutes)
            });

        return transakPrice;
    }


    public async Task<List<RampFiatItem>> GetFiatListAsync(RampFiatRequest rampFiatRequest)
    {
        try
        {
            AssertHelper.IsTrue(rampFiatRequest.Crypto.IsNullOrEmpty() || rampFiatRequest.Network.NotNullOrEmpty(),
                "Network required when Crypto exists.");
            var notSupportedFiat = new List<string>();

            // query fiat ASYNC
            var fiatTask = GetTransakFiatListWithCache(rampFiatRequest.Type);

            // query country ASYNC
            var countryTask = GetTransakCountryWithCache();

            // filter input crypto
            if (rampFiatRequest.Crypto.NotNullOrEmpty())
            {
                var cryptoList = await GetTransakCryptoListWithCache();
                var crypto = cryptoList
                    // .Where(c => c.Network != null)
                    // .Where(c => c.Network.ToNetworkId() == rampFiatRequest.Network)
                    .FirstOrDefault(c => c.Symbol == rampFiatRequest.Crypto);
                if (crypto == null) return new List<RampFiatItem>();
                notSupportedFiat.AddRange(crypto.Network?.FiatCurrenciesNotSupported ?? new List<string>());
            }

            // wait fiatTask and countryTask
            await Task.WhenAll(fiatTask, countryTask);

            var countryDict = countryTask.Result;
            var fiatList = fiatTask.Result
                .Where(f => !notSupportedFiat.Contains(f.Symbol))
                .SelectMany(f =>
                    f.SupportingCountries.Select(country => new RampFiatItem
                    {
                        Country = country,
                        Symbol = f.Symbol,
                        CountryName = countryDict.GetValueOrDefault(country)?.Name ?? country,
                        Icon = "" //TODO nzc
                    }))
                .ToList();
            return fiatList;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "{ThirdPart} GetFiatListAsync error", ThirdPart());
            return new List<RampFiatItem>();
        }
    }

    public async Task<TransakRampPrice> GetCommonRampPriceWithCache(RampDetailRequest rampDetailRequest)
    {
        var fiatList = await GetTransakFiatListWithCache(rampDetailRequest.Type);
        AssertHelper.NotEmpty(fiatList, "Transak fiat list empty");

        var fiat = fiatList
            .Where(f => f.Symbol == rampDetailRequest.Fiat)
            .Where(f => !f.SupportingCountries.IsNullOrEmpty())
            .FirstOrDefault(f => f.SupportingCountries.Contains(rampDetailRequest.Country));
        AssertHelper.NotNull(fiat, "Fiat {Fiat} not fount", rampDetailRequest.Fiat);
        AssertHelper.NotEmpty(fiat.PaymentOptions, "Fiat {Fiat} payment empty", rampDetailRequest.Fiat);

        var paymentOption = fiat.PaymentOptions
            .Where(payment => rampDetailRequest.IsBuy() || payment.IsPayOutAllowed)
            .FirstOrDefault(payment => payment.IsActive);

        // USD price
        var defaultAmount = rampDetailRequest.IsBuy()
            ? paymentOption.DefaultAmount ?? DefaultFiatAccount
            : paymentOption.DefaultAmountForPayOut ?? DefaultFiatAccount;
        rampDetailRequest.Fiat = paymentOption.LimitCurrency;
        rampDetailRequest.FiatAmount = defaultAmount;
        var limitCurrencyPrice = await GetTransakPriceWithCache(paymentOption.Id, rampDetailRequest);
        if (paymentOption.LimitCurrency == fiat.Symbol) return limitCurrencyPrice;

        // Input other fiat, calculate cryptoAmount via USD-price
        var cryptoAmount = limitCurrencyPrice.FiatAmount / limitCurrencyPrice.ConversionPrice;
        rampDetailRequest.Fiat = fiat.Symbol;
        rampDetailRequest.FiatAmount = null;
        rampDetailRequest.CryptoAmount = cryptoAmount;
        return await GetTransakPriceWithCache(paymentOption.Id, rampDetailRequest);
    }

    private async Task<TransakFiatItem> GetTransakFiatItem(string type, string fiat, [CanBeNull] string country = null)
    {
        // find fiat info 
        var fiatList = await GetTransakFiatListWithCache(type);
        AssertHelper.NotEmpty(fiatList, "Transak fiat list empty");

        var fiatItem = fiatList
            .Where(f => f.Symbol == fiat)
            .Where(f => !f.SupportingCountries.IsNullOrEmpty())
            .FirstOrDefault(f => country.IsNullOrEmpty() || f.SupportingCountries.Contains(country));
        AssertHelper.NotNull(fiatItem, "Fiat {Fiat} not fount", fiat);
        AssertHelper.NotEmpty(fiatItem.PaymentOptions, "Fiat {Fiat} payment empty", fiat);
        return fiatItem;
    }


    public async Task<RampLimitDto> GetRampLimitAsync(RampLimitRequest rampLimitRequest)
    {
        try
        {
            // find fiat info 
            var fiat = await GetTransakFiatItem(rampLimitRequest.Type, rampLimitRequest.Fiat, rampLimitRequest.Country);

            // find any payment option
            var paymentOption = fiat.MaxLimitPayment(rampLimitRequest.Type);
            AssertHelper.NotNull(paymentOption, "Fiat {Fiat} paymentOption missing", rampLimitRequest.Fiat);

            // query fiat
            var limitCurrencyFiat = paymentOption?.LimitCurrency == fiat.Symbol
                ? fiat
                : await GetTransakFiatItem(rampLimitRequest.Type, paymentOption.LimitCurrency);
            AssertHelper.NotNull(fiat, "LimitCurrencyFiat {Fiat} not fount", paymentOption.LimitCurrency);
            AssertHelper.NotEmpty(fiat.SupportingCountries, "LimitCurrencyFiat {Fiat} support country empty",
                paymentOption.LimitCurrency);

            // fiat price
            var rampDetailRequest = ObjectMapper.Map<RampLimitRequest, RampDetailRequest>(rampLimitRequest);
            var fiatPrice = await GetCommonRampPriceWithCache(rampDetailRequest);
            var cryptoFiatExchange = fiatPrice.CryptoFiatExchange();
            AssertHelper.IsTrue(cryptoFiatExchange > 0, "Invalid fiat price exchange {Ex}", fiatPrice.ConversionPrice);

            // limit currency price
            rampDetailRequest.Fiat = limitCurrencyFiat?.Symbol;
            rampDetailRequest.Country = limitCurrencyFiat?.SupportingCountries.FirstOrDefault();
            var limitCurrencyPrice = fiat.Symbol == paymentOption.LimitCurrency
                ? fiatPrice
                : await GetCommonRampPriceWithCache(rampDetailRequest);
            var limitCurrencyCryptoFiatExchange = limitCurrencyPrice.CryptoFiatExchange();
            AssertHelper.IsTrue(limitCurrencyCryptoFiatExchange > 0, "Invalid limitCurrency fiat price exchange {Ex}",
                limitCurrencyPrice.CryptoFiatExchange());

            // calculate limit amount by two exchanges
            var limitCurrencyMaxLimit = fiat.MaxLimit(rampLimitRequest.Type);
            var fiatMinLimit = CalculateLimit(limitCurrencyCryptoFiatExchange, cryptoFiatExchange,
                limitCurrencyMaxLimit.MinLimit.SafeToDecimal());
            var fiatMaxLimit = CalculateLimit(limitCurrencyCryptoFiatExchange, cryptoFiatExchange,
                limitCurrencyMaxLimit.MaxLimit.SafeToDecimal());

            // TODO nzc decimals need to test 
            var fiatLimit = new CurrencyLimit(fiat.Symbol,
                fiatMinLimit.ToString(2, DecimalHelper.RoundingOption.Ceiling),
                fiatMaxLimit.ToString(2, DecimalHelper.RoundingOption.Floor));
            var cryptoLimit = new CurrencyLimit(rampLimitRequest.Crypto,
                (cryptoFiatExchange * fiatMinLimit).ToString(8, DecimalHelper.RoundingOption.Ceiling),
                (cryptoFiatExchange * fiatMinLimit).ToString(8, DecimalHelper.RoundingOption.Floor));
            return new RampLimitDto
            {
                Fiat = fiatLimit,
                Crypto = cryptoLimit
            };
        }
        catch (Exception e)
        {
            Logger.LogError(e, "{ThirdPart} GetRampLimitAsync error", ThirdPart());
            return null;
        }
    }

    /// <param name="fromFiatExchange">e.g.: ELF-USD - 0.35</param>
    /// <param name="toFiatExchange">e.g.: ELF-EUR - 0.31</param>
    /// <param name="fromAmount">e.g.: 200 USD</param>
    private static decimal CalculateLimit(decimal fromFiatExchange, decimal toFiatExchange, decimal fromAmount)
    {
        if (fromFiatExchange == toFiatExchange) return fromAmount;
        AssertHelper.IsTrue(fromFiatExchange > 0, "Invalid fiat exchange from {Ex}", fromFiatExchange);
        AssertHelper.IsTrue(toFiatExchange > 0, "Invalid fiat exchange to {Ex}", toFiatExchange);
        return fromAmount * toFiatExchange / fromFiatExchange;
    }

    public async Task<decimal?> GetRampExchangeAsync(RampExchangeRequest rampExchangeRequest)
    {
        try
        {
            var rampDetailRequest = ObjectMapper.Map<RampExchangeRequest, RampDetailRequest>(rampExchangeRequest);
            var commonPrice = await GetCommonRampPriceWithCache(rampDetailRequest);
            return commonPrice.CryptoFiatExchange();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "{ThirdPart} GetRampExchangeAsync error", ThirdPart());
            return null;
        }
    }

    public async Task<RampPriceDto> GetRampPriceAsync(RampDetailRequest rampDetailRequest)
    {
        try
        {
            var commonPrice = await GetCommonRampPriceWithCache(rampDetailRequest);
            var transakFeePercent = commonPrice.TransakFeePercent();
            var fiatAmount = rampDetailRequest.IsBuy()
                ? rampDetailRequest.FiatAmount ?? 0
                : rampDetailRequest.CryptoAmount * commonPrice.ConversionPrice ?? 0;
            var networkFee = commonPrice.NetworkFee();
            var transakFee = fiatAmount * transakFeePercent;

            var rampPrice = ObjectMapper.Map<TransakRampPrice, RampPriceDto>(commonPrice);
            rampPrice.FeeInfo = new RampFeeInfo
            {
                NetworkFee = FeeItem.Fiat(rampDetailRequest.Fiat,
                    networkFee.ToString(2, DecimalHelper.RoundingOption.Ceiling)),
                RampFee = FeeItem.Fiat(rampDetailRequest.Fiat,
                    transakFee.ToString(2, DecimalHelper.RoundingOption.Ceiling)),
            };
            return rampPrice;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "{ThirdPart} GetRampExchangeAsync error", ThirdPart());
            return null;
        }
    }

    public async Task<ProviderRampDetailDto> GetRampDetailAsync(RampDetailRequest rampDetailRequest)
    {
        try
        {
            var fiatItem =
                await GetTransakFiatItem(rampDetailRequest.Type, rampDetailRequest.Fiat, rampDetailRequest.Country);
            var payment = fiatItem.MaxLimitPayment(rampDetailRequest.Type);
            AssertHelper.NotNull(payment, "Payment of fiatItem not found, type={}, fiat={Fiat}, country={Country}",
                rampDetailRequest.Type, rampDetailRequest.Fiat, rampDetailRequest.Country);
            
            var price = await GetTransakPriceWithCache(payment.Id, rampDetailRequest);
            
            var providerRampDetail = ObjectMapper.Map<TransakRampPrice, ProviderRampDetailDto>(price);
            providerRampDetail.FeeInfo = new RampFeeInfo
            {
                NetworkFee = FeeItem.Fiat(rampDetailRequest.Fiat,
                    price.NetworkFee().ToString(2, DecimalHelper.RoundingOption.Ceiling)),
                RampFee = FeeItem.Fiat(rampDetailRequest.Fiat,
                    price.TransakFee().ToString(2, DecimalHelper.RoundingOption.Ceiling)),
            };
            return providerRampDetail;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "{ThirdPart} GetRampExchangeAsync error", ThirdPart());
            return null;
        }
    }
}