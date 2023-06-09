using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;

namespace CAServer.ThirdPart.Alchemy;

[RemoteService(false), DisableAuditing]
public class AlchemyServiceAppService : CAServerAppService, IAlchemyServiceAppService
{
    private readonly ILogger<AlchemyServiceAppService> _logger;
    private readonly AlchemyOptions _alchemyOptions;
    private readonly IAlchemyProvider _alchemyProvider;
    private readonly IDistributedCache<List<AlchemyFiatDto>> _fiatListCache;
    private readonly IDistributedCache<AlchemyOrderQuoteDataDto> _orderQuoteCache;

    private readonly JsonSerializerSettings _setting = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public AlchemyServiceAppService(IOptions<ThirdPartOptions> merchantOptions, IAlchemyProvider alchemyProvider,
        ILogger<AlchemyServiceAppService> logger,IDistributedCache<List<AlchemyFiatDto>> fiatListCache,
        IDistributedCache<AlchemyOrderQuoteDataDto> orderQuoteCache)
    {
        _alchemyOptions = merchantOptions.Value.alchemy;
        _alchemyProvider = alchemyProvider;
        _logger = logger;
        _fiatListCache = fiatListCache;
        _orderQuoteCache = orderQuoteCache;
    }

    // get Alchemy login free token
    public async Task<AlchemyTokenDto> GetAlchemyFreeLoginTokenAsync(GetAlchemyFreeLoginTokenDto input)
    {
        try
        {
            return JsonConvert.DeserializeObject<AlchemyTokenDto>(await _alchemyProvider.HttpPost2AlchemyAsync(
                _alchemyOptions.GetTokenUri, JsonConvert.SerializeObject(input, Formatting.None, _setting)));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deserializing free login");
            throw new UserFriendlyException(e.Message);
        }
    }

    // get Alchemy fiat list
    public async Task<AlchemyFiatListDto> GetAlchemyFiatListAsync(GetAlchemyFiatListDto input)
    {
        try
        {
            var queryString = string.Join("&", input.GetType().GetProperties()
                .Select(p => $"{char.ToLower(p.Name[0]) + p.Name.Substring(1)}={p.GetValue(input)}"));

            if (input.Type != "BUY")
            {
                return await GetFiatListFromAlchemyAsync(queryString);
            }

            return new AlchemyFiatListDto
            {
                Data = await GetAlchemyFiatListAsync(CommonConstant.FiatListKey, queryString)
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deserializing fiat list.");
            throw new UserFriendlyException(e.Message);
        }
    }

    private async Task<List<AlchemyFiatDto>> GetAlchemyFiatListAsync(string key, string queryString)
    {
        return await _fiatListCache.GetOrAddAsync(
            key,
            async () => await GetFiatListDataAsync(queryString),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_alchemyOptions.FiatListExpirationMinutes)
            }
        );
    }

    private async Task<List<AlchemyFiatDto>> GetFiatListDataAsync(string queryString)
    {
        var result = await GetFiatListFromAlchemyAsync(queryString);
        return result.Data;
    }

    private async Task<AlchemyFiatListDto> GetFiatListFromAlchemyAsync(string queryString)
    {
        return JsonConvert.DeserializeObject<AlchemyFiatListDto>(
            await _alchemyProvider.HttpGetFromAlchemy(_alchemyOptions.FiatListUri + "?" + queryString));
    }

    // get Alchemy cryptoList 
    public async Task<AlchemyCryptoListDto> GetAlchemyCryptoListAsync(GetAlchemyCryptoListDto input)
    {
        try
        {
            string queryString = string.Join("&", input.GetType().GetProperties()
                .Select(p => $"{char.ToLower(p.Name[0]) + p.Name.Substring(1)}={p.GetValue(input)}"));

            return JsonConvert.DeserializeObject<AlchemyCryptoListDto>(
                await _alchemyProvider.HttpGetFromAlchemy(_alchemyOptions.CryptoListUri + "?" + queryString));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deserializing crypto list.");
            throw new UserFriendlyException(e.Message);
        }
    }

    // post Alchemy cryptoList
    public async Task<AlchemyOrderQuoteResultDto> GetAlchemyOrderQuoteAsync(GetAlchemyOrderQuoteDto input)
    {
        try
        {
            var key = $"{input.Crypto}.{input.Network}.{input.Fiat}.{input.Country}";
            if (input.Side == "BUY")
            {
                return new AlchemyOrderQuoteResultDto
                {
                    Data = await GetBuyOrderQuoteAsync(key, input)
                };
            }

            key += $".{input.Amount}";
            return new AlchemyOrderQuoteResultDto
            {
                Data = await GetOrderQuoteAsync(key, input)
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deserializing order quote.");
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
            async () => await GetOrderQuoteFromAlchemyAsync(input),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_alchemyOptions.OrderQuoteExpirationMinutes)
            }
        );
    }

    private async Task<AlchemyOrderQuoteDataDto> GetOrderQuoteFromAlchemyAsync(GetAlchemyOrderQuoteDto input)
    {
        var result = JsonConvert.DeserializeObject<AlchemyOrderQuoteResultDto>(
            await _alchemyProvider.HttpPost2AlchemyAsync(_alchemyOptions.OrderQuoteUri,
                JsonConvert.SerializeObject(input, Formatting.None, _setting)));
        return result.Data;
    }

    // generate alchemy signature
    public async Task<AlchemySignatureResultDto> GetAlchemySignatureAsync(GetAlchemySignatureDto input)
    {
        try
        {
            return new AlchemySignatureResultDto()
            {
                Signature = AlchemyHelper.AesEncrypt($"address={input.Address}&appId={_alchemyOptions.AppId}",
                    _alchemyOptions.AppSecret)
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Alchemy signature AES encrypting exception");
            return new AlchemySignatureResultDto()
            {
                Success = "Fail",
                ReturnMsg = $"Error AES encrypting, error msg is {e.Message}"
            };
        }
    }
}