using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.MultiToken;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart.Alchemy;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Ramp;
using CAServer.ThirdPart.Dtos.ThirdPart;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Serilog;

namespace CAServer.ThirdPart.Adaptor;

public class AlchemyAdaptor : CAServerAppService, IThirdPartAdaptor
{
    private const decimal DefaultAmount = 200;
    private const string CryptoCacheKey = "Ramp:transak:crypto:";
    private readonly IAlchemyServiceAppService _alchemyServiceAppService;
    private readonly IOptionsMonitor<RampOptions> _rampOptions;

    public AlchemyAdaptor(IAlchemyServiceAppService alchemyServiceAppService,
        IOptionsMonitor<RampOptions> rampOptions)
    {
        _alchemyServiceAppService = alchemyServiceAppService;
        _rampOptions = rampOptions;
    }


    public string ThirdPart()
    {
        return ThirdPartNameType.Alchemy.ToString();
    }

    private ThirdPartProvider AlchemyProviderOption()
    {
        return _rampOptions.CurrentValue.Providers[ThirdPart()];
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
            return rampLimitRequest.IsBuy()
                ? await AlchemyOnRampLimit(rampLimitRequest)
                : await AlchemyOffRampLimit(rampLimitRequest);
        }
        catch (Exception e)
        {
            Log.Error(e, "{ThirdPart} GetRampLimitAsync ERROR", ThirdPart());
            return null;
        }
    }

    private async Task<RampLimitDto> AlchemyOnRampLimit(RampLimitRequest rampDetailRequest)
    {
        var alchemyFiatList = await _alchemyServiceAppService.GetAlchemyFiatListWithCacheAsync(new GetAlchemyFiatListDto
        {
            Type = rampDetailRequest.Type
        });
        AssertHelper.IsTrue(alchemyFiatList.Success, "Fiat list query failed.");

        var fiatItem = alchemyFiatList.Data.FirstOrDefault(fiat => fiat.Currency == rampDetailRequest.Fiat);
        AssertHelper.NotNull(fiatItem, "Fiat {Currency} not found in fiat list", rampDetailRequest.Fiat);

        return new RampLimitDto
        {
            Fiat = new CurrencyLimit(rampDetailRequest.Fiat, fiatItem?.PayMin, fiatItem?.PayMax)
        };
    }


    private async Task<RampLimitDto> AlchemyOffRampLimit(RampLimitRequest rampDetailRequest)
    {
        var alchemyCryptoList = await _alchemyServiceAppService.GetAlchemyCryptoListAsync(new GetAlchemyCryptoListDto
        {
            Fiat = rampDetailRequest.Fiat
        });
        AssertHelper.IsTrue(alchemyCryptoList.Success, "Crypto list query failed.");

        var cryptoItem = alchemyCryptoList.Data.FirstOrDefault(crypto => crypto.Crypto == rampDetailRequest.Crypto);
        AssertHelper.NotNull(cryptoItem, "Crypto {Crypto} not found", rampDetailRequest.Crypto);

        return new RampLimitDto
        {
            Crypto = new CurrencyLimit(rampDetailRequest.Crypto, cryptoItem?.MinSellAmount, cryptoItem?.MaxSellAmount)
        };
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
        var alchemyFiatList = await _alchemyServiceAppService.GetAlchemyFiatListWithCacheAsync(new GetAlchemyFiatListDto
        {
            Type = input.ToString()
        });
        AssertHelper.IsTrue(alchemyFiatList.Success, "Fiat list empty");

        var fiatItem = alchemyFiatList.Data.FirstOrDefault(fiat => fiat.Currency == input.Fiat);
        AssertHelper.NotNull(fiatItem, "Fiat {Currency} not found in fiat list", input.Fiat);

        // query order quote with a valid amount
        var amount = (fiatItem?.PayMin ?? "0").SafeToDecimal();
        input.Amount = (amount > 0 ? amount : DefaultAmount).ToString(CultureInfo.InvariantCulture);
        var orderQuote = await _alchemyServiceAppService.GetAlchemyOrderQuoteAsync(input);
        AssertHelper.IsTrue(orderQuote.Success, "Order quote empty");

        return orderQuote.Data;
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
            var alchemyOrderQuoteDto = ObjectMapper.Map<RampDetailRequest, GetAlchemyOrderQuoteDto>(rampDetailRequest);
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
            var orderQuote = await _alchemyServiceAppService.GetAlchemyOrderQuoteAsync(alchemyOrderQuoteDto);
            var rampPrice = ObjectMapper.Map<AlchemyOrderQuoteDataDto, ProviderRampDetailDto>(orderQuote.Data);
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
            Log.Error(e, "{ThirdPart} GetRampDetailAsync ERROR", ThirdPart());
            return null;
        }
    }
}