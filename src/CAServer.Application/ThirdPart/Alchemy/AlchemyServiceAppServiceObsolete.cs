using System;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.ThirdPart.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;

namespace CAServer.ThirdPart.Alchemy;

public partial class AlchemyServiceAppService
{
    
    // get Alchemy fiat list
    public async Task<AlchemyFiatListResponseDto> GetAlchemyFiatListAsync(GetAlchemyFiatListDto input)
    {
        try
        {
            return input.Type != OrderTransDirect.BUY.ToString() 
                ? await _alchemyProvider.GetAlchemyFiatList(input)
                : new AlchemyFiatListResponseDto 
                {
                    Data = await GetAlchemyFiatListWithCacheAsync(CommonConstant.FiatListKey, input)
                };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deserializing fiat list");
            throw new UserFriendlyException(e.Message);
        }
    }

    // post Alchemy cryptoList
    public async Task<AlchemyOrderQuoteResponseDto> GetAlchemyOrderQuoteAsync(GetAlchemyOrderQuoteDto input)
    {
        try
        {
            var key = $"{input.Crypto}.{input.Network}.{input.Fiat}.{input.Country}";
            if (input.Side == "BUY")
            {
                return new AlchemyOrderQuoteResponseDto
                {
                    Data = await GetBuyOrderQuoteAsync(key, input)
                };
            }

            key += $".{input.Amount}";
            return new AlchemyOrderQuoteResponseDto
            {
                Data = await GetOrderQuoteAsync(key, input)
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deserializing order quote");
            throw new UserFriendlyException(e.Message);
        }
    }

    // get Alchemy cryptoList 
    public async Task<AlchemyCryptoListResponseDto> GetAlchemyCryptoListAsync(GetAlchemyCryptoListDto input)
    {
        try
        {
            return await _alchemyProvider.GetAlchemyCryptoList(input);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deserializing crypto list");
            throw new UserFriendlyException(e.Message);
        }
    }

    private async Task<AlchemyOrderQuoteDataDto> GetBuyOrderQuoteAsync(string key, GetAlchemyOrderQuoteDto input)
    {
        var quoteData = await _orderQuoteCache.GetAsync(key);
        if (quoteData == null)
        {
            return await GetOrderQuoteAsync(key, input);
        }

        var fiatListData = await _fiatListCache.GetAsync(CommonConstant.FiatListKey);
        if (fiatListData == null || fiatListData.Count == 0)
        {
            var fiatList = await GetAlchemyFiatListAsync(new GetAlchemyFiatListDto());
            fiatListData = fiatList.Data;
        }

        var fiat = fiatListData.FirstOrDefault(t => t.Currency == input.Fiat);
        if (fiat == null)
        {
            throw new UserFriendlyException("Get fiat list data fail.");
        }

        double.TryParse(input.Amount, out var amount);
        double.TryParse(fiat.FixedFee, out var fixedFee);
        double.TryParse(fiat.FeeRate, out var feeRate);
        double.TryParse(quoteData.NetworkFee, out var networkFee);
        double.TryParse(quoteData.CryptoPrice, out var cryptoPrice);
        var rampFee = fixedFee + amount * feeRate;
        quoteData.RampFee = rampFee.ToString("f2");
        quoteData.CryptoQuantity = ((amount - rampFee - networkFee) / cryptoPrice).ToString("f8");

        return quoteData;
    }

    private async Task<AlchemyOrderQuoteDataDto> GetOrderQuoteAsync(string key, GetAlchemyOrderQuoteDto input)
    {
        return await _orderQuoteCache.GetOrAddAsync(
            key,
            async () => (await _alchemyProvider.QueryAlchemyOrderQuoteList(input)).Data,
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_alchemyOptions.OrderQuoteExpirationMinutes)
            }
        );
    }

}