using System;
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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CAServer.ThirdPart.Adaptor;

public class TransakAdaptor : CAServerAppService, IThirdPartAdaptor
{
    private const string CryptoCacheKey = "Ramp:transak:crypto";
    private const string FiatCacheKey = "Ramp:transak:fiat";
    private const string CountryCacheKey = "Ramp:transak:country";
    private Func<string, string, string> _priceCachekey = (crypto, fiat) => "Ramp:transak:price:" + crypto + ":" + fiat;
    private const decimal DefaultFiatAccount = 200;

    private readonly TransakProvider _transakProvider;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly IMemoryCache<List<TransakCryptoItem>> _cryptoCache;
    private readonly IMemoryCache<List<TransakFiatItem>> _fiatCache;
    private readonly IMemoryCache<TransakRampPrice> _rampPrice;
    private readonly IMemoryCache<Dictionary<string, TransakCountry>> _countryCache;

    public TransakAdaptor(TransakProvider transakProvider, IOptionsMonitor<ThirdPartOptions> thirdPartOptions,
        IMemoryCache<List<TransakCryptoItem>> cryptoCache, IMemoryCache<List<TransakFiatItem>> fiatCache,
        IMemoryCache<Dictionary<string, TransakCountry>> countryCache, IMemoryCache<TransakRampPrice> rampPrice)
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
            async () => await _transakProvider.GetFiatCurrenciesAsync(),
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
    private async Task<RampPriceDto> GetTransakPriceWithCache(string paymentMethod, RampDetailRequest rampPriceRequest)
    {
        var transakPriceRequest = ObjectMapper.Map<RampDetailRequest, GetRampPriceRequest>(rampPriceRequest);
        transakPriceRequest.PaymentMethod = paymentMethod;
        
        // query transak price
        var transakPrice = await _rampPrice.GetOrAddAsync(_priceCachekey(rampPriceRequest.Crypto, rampPriceRequest.Fiat),
            async () => await _transakProvider.GetRampPriceAsync(transakPriceRequest), 
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration =
                    DateTimeOffset.Now.AddMinutes(_thirdPartOptions.CurrentValue.Transak.OrderQuoteExpirationMinutes)
            });
        
        var rampPrice = ObjectMapper.Map<TransakRampPrice, RampPriceDto>(transakPrice);
        var transakFee = transakPrice.FeeBreakdown.FirstOrDefault(fee => fee.Id == TransakFeeName.TransakFee);
        var networkFee = transakPrice.FeeBreakdown.FirstOrDefault(fee => fee.Id == TransakFeeName.NetworkFee);
        rampPrice.FeeInfo = new RampFeeInfo
        {
            RampFee = transakFee == null ? null : new FeeItem
            {
                Type = CommonConstant.CurrencyFiat,
                Amount = transakFee.Value.ToString(CultureInfo.InvariantCulture),
                Symbol = transakPriceRequest.FiatCurrency
            },
            NetworkFee = networkFee == null ? null : new FeeItem
            {
                Type = CommonConstant.CurrencyFiat,
                Amount = networkFee.Value.ToString(CultureInfo.InvariantCulture),
                Symbol = transakPriceRequest.FiatCurrency
            }
        };
        return rampPrice;
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

    public async Task<RampPriceDto> GetCommonRampPriceWithCache(RampDetailRequest rampDetailRequest)
    {
        var fiatList = await GetTransakFiatListWithCache(rampDetailRequest.Type);
        AssertHelper.NotEmpty(fiatList, "Transak fiat list empty");
        
        var fiat = fiatList.FirstOrDefault(f => f.Symbol == rampDetailRequest.Fiat);
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

        // Input another fiat, calculate cryptoAmount via USD-price
        var cryptoAmount = limitCurrencyPrice.FiatAmount.SafeToDecimal() / limitCurrencyPrice.Exchange.SafeToDecimal();
        rampDetailRequest.Fiat = fiat.Symbol;
        rampDetailRequest.FiatAmount = null;
        rampDetailRequest.CryptoAmount = cryptoAmount;
        return await GetTransakPriceWithCache(paymentOption.Id, rampDetailRequest);
    }
    

    public async Task<RampLimitDto> GetRampLimitAsync(RampLimitRequest rampDetailRequest)
    {
        throw new System.NotImplementedException();
    }

    public async Task<decimal?> GetRampExchangeAsync(RampExchangeRequest rampDetailRequest)
    {
        throw new System.NotImplementedException();
    }

    public async Task<RampPriceDto> GetRampPriceAsync(RampDetailRequest rampDetailRequest)
    {
        throw new System.NotImplementedException();
    }

    public async Task<ProviderRampDetailDto> GetRampDetailAsync(RampDetailRequest rampDetailRequest)
    {
        throw new System.NotImplementedException();
    }
}