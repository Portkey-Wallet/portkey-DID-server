using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.ThirdPart.Dtos;

namespace CAServer.ThirdPart.Alchemy;

public partial class AlchemyServiceAppService
{
    public string MerchantName()
    {
        return MerchantNameType.Alchemy.ToString();
    }

    public async Task<QueryFiatResponseDto> GetMerchantFiatAsync(QueryFiatRequestDto input)
    {
        var query = new GetAlchemyFiatListDto() { Type = input.Type };

        // BUY-query from cache, SELL-query invoke Alchemy direct
        var alchemyFiatList = input.Type == OrderTransDirect.BUY.ToString()
            ? await GetAlchemyFiatListWithCacheAsync(CommonConstant.FiatListKey, query)
            : (await _alchemyProvider.GetAlchemyFiatList(query)).Data;

        var fiatDict = new Dictionary<Tuple<string, string>, FiatItem>();
        foreach (var alchemyFiat in alchemyFiatList)
        {
            var fiatCountry = Tuple.Create(alchemyFiat.Currency, alchemyFiat.Country);
            var fiatItem = fiatDict.GetOrAdd(fiatCountry, () => new FiatItem()
            {
                Fiat = alchemyFiat.Currency,
                Country = alchemyFiat.Country
            });
            fiatItem.FiatPayments.Add(new FiatPayment()
            {
                PaymentCode = alchemyFiat.PayWayCode,
                LimitCurrency = alchemyFiat.Currency,
                MinAmount = alchemyFiat.PayMin,
                MaxAmount = alchemyFiat.PayMax,
            });
        }

        return new QueryFiatResponseDto()
        {
            FiatList = fiatDict.Values.OrderBy(val => val.Fiat).ToList()
        };
    }

    public async Task<QueryCryptoResponseDto> GetMerchantCryptoAsync(QueryCurrencyRequestDto input)
    {
        var fiatResp = await GetMerchantFiatAsync(new QueryFiatRequestDto { Type = input.Type });
        var alchemyCryptoResp = await _alchemyProvider.GetAlchemyCryptoList(new GetAlchemyCryptoListDto()
        {
            Fiat = input.Fiat
        });

        // LimitAmount data will be discarded. The quota logic is uniformly handled within the GetMerchantPrice step.
        var cryptoList = alchemyCryptoResp.Data
            .Where(c => input.Crypto.IsNullOrEmpty() || input.Crypto == c.Crypto)
            .Select(c => new CryptoItem()
            {
                CryptoSymbol = c.Crypto,
                Network = c.Network,
                Icon = c.Icon
            })
            .ToList();
        
        var fiatList = fiatResp.FiatList
            .Where(f => input.Fiat.IsNullOrEmpty() || input.Fiat == f.Fiat)
            .ToList();
        
        foreach (var cryptoItem in cryptoList)
        {
            cryptoItem.FiatList = fiatList;
        }

        return new QueryCryptoResponseDto()
        {
            CryptoList = cryptoList
        };
    }

    public async Task<QueryPriceResponseDto> GetMerchantPriceAsync(QueryPriceRequestDto input)
    {
        //TODO
        throw new NotImplementedException();
    }
}