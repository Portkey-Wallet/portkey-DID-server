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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.ThirdPart.Adaptor;

[DisableAuditing]
public class AlchemyAdaptor : CAServerAppService, IThirdPartAdaptor
{
    private const decimal DefaultAmount = 200;
    private readonly IAlchemyServiceAppService _alchemyServiceAppService;
    private readonly IOptionsMonitor<RampOptions> _rampOptions;
    private readonly ILogger<AlchemyAdaptor> _logger;

    public AlchemyAdaptor(IAlchemyServiceAppService alchemyServiceAppService,
        IOptionsMonitor<RampOptions> rampOptions,
        ILogger<AlchemyAdaptor> logger)
    {
        _alchemyServiceAppService = alchemyServiceAppService;
        _rampOptions = rampOptions;
        _logger = logger;
    }


    public string ThirdPart()
    {
        return ThirdPartNameType.Alchemy.ToString();
    }

    private ThirdPartProvider AlchemyProviderOption()
    {
        return _rampOptions.CurrentValue.Providers[ThirdPart()];
    }
    
    public string MappingToAlchemyNetwork(string network)
    {
        if (network.IsNullOrEmpty()) return network;
        var mappingExists = _rampOptions.CurrentValue.Provider(ThirdPartNameType.Alchemy).NetworkMapping
            .TryGetValue(network, out var mappingNetwork);
        return mappingExists ? mappingNetwork : network;
    }
    
    public string MappingFromAlchemyNetwork(string network)
    {
        if (network.IsNullOrEmpty()) return network;
        var mappingNetwork = _rampOptions.CurrentValue.Provider(ThirdPartNameType.Alchemy).NetworkMapping
            .FirstOrDefault(kv => kv.Value == network);
        return mappingNetwork.Key.DefaultIfEmpty(network);
    }

    public string MappingToAlchemySymbol(string symbol)
    {
        if (symbol.IsNullOrEmpty()) return symbol;
        var mappingExists = _rampOptions.CurrentValue.Provider(ThirdPartNameType.Alchemy).SymbolMapping
            .TryGetValue(symbol, out var achSymbol);
        return mappingExists ? achSymbol : symbol;
    }

    public string MappingFromAchSymbol(string symbol)
    {
        if (symbol.IsNullOrEmpty()) return symbol;
        var mappingNetwork = _rampOptions.CurrentValue.Provider(ThirdPartNameType.Alchemy).SymbolMapping
            .FirstOrDefault(kv => kv.Value == symbol);
        return mappingNetwork.Key.DefaultIfEmpty(symbol);
    }


    /// <summary>
    ///     Get fiat list
    /// </summary>
    /// <param name="rampFiatRequest"></param>
    /// <returns></returns>
    public async Task<List<RampFiatItem>> GetFiatListAsync(RampFiatRequest rampFiatRequest)
    {
        try
        {
            var alchemyFiatList = await _alchemyServiceAppService.GetAlchemyFiatListWithCacheAsync(
                new GetAlchemyFiatListDto
                {
                    Type = rampFiatRequest.Type
                });
            AssertHelper.IsTrue(alchemyFiatList.Success, "GetFiatListAsync error {Msg}", alchemyFiatList.Message);
            var rampFiatList = alchemyFiatList.Data.Select(f => new RampFiatItem()
            {
                Country = f.Country,
                Symbol = f.Currency,
                CountryName = f.CountryName,
                Icon = AlchemyProviderOption().CountryIconUrl.ReplaceWithDict(new Dictionary<string, string>
                {
                    ["ISO"] = f.Country
                })
            }).ToList();
            return rampFiatList;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "{ThirdPart} GetFiatList error", ThirdPart());
            return new List<RampFiatItem>();
        }
    }

    private List<AlchemyFiatDto> MatchFiatDto(List<AlchemyFiatDto> fiatList, string fiatCurrency, string country)
    {
        return fiatList
            .Where(fiat => fiatCurrency.IsNullOrEmpty() || fiat.Currency == fiatCurrency)
            .Where(fiat => country.IsNullOrEmpty() || fiat.Country == country)
            .ToList();
    }

    public async Task<List<TransakCryptoItem>> GetCryptoCurrenciesAsync()
    {
        return new List<TransakCryptoItem>();
    }

    public async Task<List<RampCurrencyItem>> GetCryptoListAsync(RampCryptoRequest request)
    {
        try
        {
            if (request.Fiat.NotNullOrEmpty())
            {
                var alchemyFiatList = await _alchemyServiceAppService.GetAlchemyFiatListWithCacheAsync(
                    new GetAlchemyFiatListDto { Type = request.Type });
                _logger.LogInformation("GetCryptoListAsync {0} request:{1} response:{2}", ThirdPart(),
                    JsonConvert.SerializeObject(request), JsonConvert.SerializeObject(alchemyFiatList));
                AssertHelper.IsTrue(alchemyFiatList.Success, "GetFiatListAsync error {Msg}", alchemyFiatList.Message);
                var matchFiatList = MatchFiatDto(alchemyFiatList.Data, request.Fiat, request.Country);
                AssertHelper.NotEmpty(matchFiatList, "Fiat not support {}-{}", request.Fiat, request.Country);
            }

            var alchemyCryptoList = await _alchemyServiceAppService.GetAlchemyCryptoListAsync(
                new GetAlchemyCryptoListDto { Fiat = request.Fiat });
            _logger.LogInformation("GetCryptoListAsync alchemyCryptoList {0} request:{1} response:{2}", ThirdPart(),
                JsonConvert.SerializeObject(request), JsonConvert.SerializeObject(alchemyCryptoList));
            AssertHelper.IsTrue(alchemyCryptoList.Success, "Crypto list query failed.");

            var alchemyNetwork = MappingToAlchemyNetwork(request.Network);
            var cryptoItem = alchemyCryptoList.Data
                .Where(c => string.IsNullOrEmpty(alchemyNetwork) || c.Network == alchemyNetwork)
                .Where(c => request.IsBuy ? c.BuyEnable.SafeToInt() > 0 : c.SellEnable.SafeToInt() > 0)
                .GroupBy(c => string.Join(CommonConstant.Underline, c.Crypto, c.Network))
                .Select(g => g.First());
            
            var rest = cryptoItem.Select(item => new RampCurrencyItem()
            {
                Symbol = MappingFromAchSymbol(item.Crypto),
                Network = MappingFromAlchemyNetwork(item.Network),
            }).ToList();
            _logger.LogInformation("GetCryptoListAsync alchemyCryptoList cryptoItem {0} request:{1} response:{2}", ThirdPart(),
                JsonConvert.SerializeObject(request), JsonConvert.SerializeObject(rest));
            return rest;
        }
        catch (UserFriendlyException e)
        {
            Log.Warning(e, "{ThirdPart} GetCryptoListAsync failed", ThirdPart());
        }
        catch (Exception e)
        {
            Log.Error(e, "{ThirdPart} GetCryptoListAsync ERROR", ThirdPart());
        }

        return new List<RampCurrencyItem>();
    }

    /// <summary>
    ///     Get ramp limit
    /// </summary>
    /// <param name="rampLimitRequest"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<RampLimitDto> GetRampLimitAsync(RampLimitRequest rampLimitRequest)
    {
        try
        {
            var (min, max) =
                await GetRampLimit(rampLimitRequest.Fiat, rampLimitRequest.Crypto, rampLimitRequest.Network, rampLimitRequest.IsBuy());
            
            return new RampLimitDto
            {
                Fiat = rampLimitRequest.IsBuy()
                    ? new CurrencyLimit(rampLimitRequest.Fiat, min, max)
                    : null,
                Crypto = rampLimitRequest.IsBuy()
                    ? null
                    : new CurrencyLimit(rampLimitRequest.Crypto, min, max)
            };
        }
        catch (Exception e)
        {
            Log.Error(e, "{ThirdPart} GetRampLimitAsync ERROR", ThirdPart());
            return null;
        }
    }


    private async Task<Tuple<string, string>> GetRampLimit(string fiat, string crypto, string network, bool isBuy)
    {
        var alchemyCryptoList = await _alchemyServiceAppService.GetAlchemyCryptoListAsync(
            new GetAlchemyCryptoListDto
            {
                Fiat = fiat
            });
        AssertHelper.IsTrue(alchemyCryptoList.Success, "Crypto list query failed.");

        var alchemyNetwork = MappingToAlchemyNetwork(network);
        var alchemySymbol = MappingToAlchemySymbol(crypto);
        var cryptoItem = alchemyCryptoList.Data
            .Where(c => c.Crypto == alchemySymbol)
            .Where(c => c.Network == alchemyNetwork)
            .FirstOrDefault(c => isBuy ? c.BuyEnable.SafeToInt() > 0 : c.SellEnable.SafeToInt() > 0);
        AssertHelper.NotNull(cryptoItem, "Crypto {Crypto} not support", crypto);

        var min = isBuy ? cryptoItem?.MinPurchaseAmount : cryptoItem?.MinSellAmount;
        var max = isBuy ? cryptoItem?.MaxPurchaseAmount : cryptoItem?.MaxSellAmount;
        AssertHelper.IsTrue(max.SafeToDecimal() > 0 && max.SafeToDecimal() - min.SafeToDecimal() > 0,
            "Alchemy limit invalid, min={Min}, max={Max}, fiat={Fiat}, Crypto={Crypto}",
            min, max, fiat, crypto);
        return Tuple.Create(min, max);
    }


    /// <summary>
    ///     Get ramp exchange
    /// </summary>
    /// <param name="rampExchangeRequest"></param>
    /// <returns></returns>
    public async Task<decimal?> GetRampExchangeAsync(RampExchangeRequest rampExchangeRequest)
    {
        try
        {
            var alchemyOrderQuoteDto =
                ObjectMapper.Map<RampExchangeRequest, GetAlchemyOrderQuoteDto>(rampExchangeRequest);
            alchemyOrderQuoteDto.Crypto = MappingToAlchemySymbol(alchemyOrderQuoteDto.Crypto);
            alchemyOrderQuoteDto.Network = MappingToAlchemyNetwork(alchemyOrderQuoteDto.Network);
            var orderQuote = await GetCommonAlchemyOrderQuoteData(alchemyOrderQuoteDto);
            return orderQuote.CryptoPrice.SafeToDecimal();
        }
        catch (Exception e)
        {
            Log.Error(e, "{ThirdPart} GetRampExchangeAsync ERROR", ThirdPart());
            return null;
        }
    }


    private async Task<AlchemyOrderQuoteDataDto> GetCommonAlchemyOrderQuoteData(GetAlchemyOrderQuoteDto input)
    {
        var (min, _) = await GetRampLimit(input.Fiat, input.Crypto, input.Network, input.IsBuy());

        // query order quote with a valid amount
        var amount = (min ?? "0").SafeToDecimal();
        input.Amount = (amount > 0 ? amount : DefaultAmount).ToString(CultureInfo.InvariantCulture);
        var orderQuote = await _alchemyServiceAppService.GetAlchemyOrderQuoteAsync(input);
        AssertHelper.IsTrue(orderQuote.Success, "Order quote empty");

        return orderQuote.Data;
    }

    private async Task<bool> InPriceLimit(RampDetailRequest rampDetailRequest)
    {
        var (min, max) =
            await GetRampLimit(rampDetailRequest.Fiat, rampDetailRequest.Crypto, rampDetailRequest.Network, rampDetailRequest.IsBuy());
        var amount = (rampDetailRequest.IsBuy() ? rampDetailRequest.FiatAmount : rampDetailRequest.CryptoAmount) ?? 0;

        return amount >= min.SafeToDecimal() && amount <= max.SafeToDecimal();
    }

    /// <summary>
    ///     Get ramp price
    /// </summary>
    /// <param name="rampDetailRequest"></param>
    /// <returns></returns>
    public async Task<RampPriceDto> GetRampPriceAsync(RampDetailRequest rampDetailRequest)
    {
        try
        {
            if (!await InPriceLimit(rampDetailRequest)) return null;

            var alchemyOrderQuoteDto = ObjectMapper.Map<RampDetailRequest, GetAlchemyOrderQuoteDto>(rampDetailRequest);
            alchemyOrderQuoteDto.Crypto = MappingToAlchemySymbol(alchemyOrderQuoteDto.Crypto);
            alchemyOrderQuoteDto.Network = MappingToAlchemyNetwork(alchemyOrderQuoteDto.Network);
            var orderQuote = await _alchemyServiceAppService.GetAlchemyOrderQuoteAsync(alchemyOrderQuoteDto);
            AssertHelper.IsTrue(orderQuote.Success, "Query Alchemy order quote failed, " + orderQuote.Message);

            var rampPrice = ObjectMapper.Map<AlchemyOrderQuoteDataDto, RampPriceDto>(orderQuote.Data);
            rampPrice.ThirdPart = ThirdPart();
            rampPrice.FeeInfo = new RampFeeInfo
            {
                RampFee = FeeItem.Fiat(orderQuote.Data.Fiat, orderQuote.Data.RampFee),
                NetworkFee = FeeItem.Fiat(orderQuote.Data.Fiat, orderQuote.Data.NetworkFee),
            };
            return rampPrice;
        }
        catch (Exception e)
        {
            Log.Error(e, "{ThirdPart} GetRampPriceAsync ERROR", ThirdPart());
            return null;
        }
    }


    /// <summary>
    ///     Get ramp detail
    /// </summary>
    /// <param name="rampDetailRequest"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<ProviderRampDetailDto> GetRampDetailAsync(RampDetailRequest rampDetailRequest)
    {
        try
        {
            var alchemyOrderQuoteDto = ObjectMapper.Map<RampDetailRequest, GetAlchemyOrderQuoteDto>(rampDetailRequest);
            if (!await InPriceLimit(rampDetailRequest)) return null;

            alchemyOrderQuoteDto.Crypto = MappingToAlchemySymbol(alchemyOrderQuoteDto.Crypto);
            alchemyOrderQuoteDto.Network = MappingToAlchemyNetwork(alchemyOrderQuoteDto.Network);
            var orderQuote = await _alchemyServiceAppService.GetAlchemyOrderQuoteAsync(alchemyOrderQuoteDto);
            var rampPrice = ObjectMapper.Map<AlchemyOrderQuoteDataDto, ProviderRampDetailDto>(orderQuote.Data);
            rampPrice.ThirdPart = ThirdPart();
            rampPrice.ProviderNetwork = orderQuote.Data.Network;
            rampPrice.ProviderSymbol = MappingToAlchemySymbol(rampDetailRequest.Crypto);
            rampPrice.FeeInfo = new RampFeeInfo
            {
                RampFee = FeeItem.Fiat(orderQuote.Data.Fiat, orderQuote.Data.RampFee),
                NetworkFee = FeeItem.Fiat(orderQuote.Data.Fiat, orderQuote.Data.NetworkFee),
            };
            return rampPrice;
        }
        catch (Exception e)
        {
            Log.Error(e, "{ThirdPart} GetRampDetailAsync ERROR", ThirdPart());
            return null;
        }
    }
}