using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Provider;
using JetBrains.Annotations;
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
    private readonly AlchemyProvider _alchemyProvider;
    private readonly IDistributedCache<List<AlchemyFiatDto>> _fiatListCache;
    private readonly IDistributedCache<List<AlchemyFiatDto>> _nftFiatListCache;
    private readonly IDistributedCache<AlchemyOrderQuoteDataDto> _orderQuoteCache;

    private readonly JsonSerializerSettings _setting = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public AlchemyServiceAppService(IOptions<ThirdPartOptions> thirdPartOptions, AlchemyProvider alchemyProvider,
        ILogger<AlchemyServiceAppService> logger, IDistributedCache<List<AlchemyFiatDto>> fiatListCache,
        IDistributedCache<AlchemyOrderQuoteDataDto> orderQuoteCache,
        IDistributedCache<List<AlchemyFiatDto>> nftFiatListCache)
    {
        _alchemyOptions = thirdPartOptions.Value.Alchemy;
        _alchemyProvider = alchemyProvider;
        _logger = logger;
        _fiatListCache = fiatListCache;
        _orderQuoteCache = orderQuoteCache;
        _nftFiatListCache = nftFiatListCache;
    }

    // get Alchemy login free token
    public async Task<CommonResponseDto<AlchemyTokenDataDto>> GetAlchemyFreeLoginTokenAsync(
        GetAlchemyFreeLoginTokenDto input)
    {
        try
        {
            return new CommonResponseDto<AlchemyTokenDataDto>(
                await _alchemyProvider.GetAlchemyRampFreeLoginToken(input));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetAlchemyFreeLoginTokenAsync error");
            return new CommonResponseDto<AlchemyTokenDataDto>().Error(e, "Get alchemy free login token fail.");
        }
    }

    public async Task<AlchemyBaseResponseDto<AlchemyTokenDataDto>> GetAlchemyNftFreeLoginTokenAsync(
        GetAlchemyFreeLoginTokenDto input)
    {
        try
        {
            var resp = await _alchemyProvider.GetNftFreeLoginToken(input);
            AssertHelper.NotEmpty(resp.AccessToken, "AccessToken empty");
            return new AlchemyBaseResponseDto<AlchemyTokenDataDto>(resp);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Get alchemy nft free login token failed");
            throw new UserFriendlyException("Get token failed, please try again later");
        }
    }

    // get Alchemy fiat list
    public async Task<CommonResponseDto<List<AlchemyFiatDto>>> GetAlchemyFiatListAsync(GetAlchemyFiatListDto input)
    {
        try
        {
            return new CommonResponseDto<List<AlchemyFiatDto>>(await GetAlchemyFiatListWithCacheAsync(input));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deserializing fiat list");
            return new CommonResponseDto<List<AlchemyFiatDto>>().Error(e, "Get Alchemy fiat list failed");
        }
    }

    public async Task<List<AlchemyFiatDto>> GetAlchemyNftFiatListAsync()
    {
        return await _nftFiatListCache.GetOrAddAsync(CommonConstant.NftFiatListKey,
            async () => await _alchemyProvider.GetNftFiatList(),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_alchemyOptions.NftFiatListExpirationMinutes)
            }
        );
    }

    private async Task<List<AlchemyFiatDto>> GetAlchemyFiatListWithCacheAsync(GetAlchemyFiatListDto input)
    {
        return await _fiatListCache.GetOrAddAsync(GrainIdHelper.GenerateGrainId(CommonConstant.FiatListKey, input.Type),
            async () => await _alchemyProvider.GetAlchemyFiatList(input),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_alchemyOptions.FiatListExpirationMinutes)
            }
        );
    }

    // get Alchemy cryptoList 
    public async Task<AlchemyBaseResponseDto<List<AlchemyCryptoDto>>> GetAlchemyCryptoListAsync(
        GetAlchemyCryptoListDto input)
    {
        try
        {
            string queryString = string.Join("&", input.GetType().GetProperties()
                .Select(p => $"{char.ToLower(p.Name[0]) + p.Name.Substring(1)}={p.GetValue(input)}"));

            return JsonConvert.DeserializeObject<AlchemyBaseResponseDto<List<AlchemyCryptoDto>>>(
                await _alchemyProvider.HttpGetFromAlchemy(_alchemyOptions.CryptoListUri + "?" + queryString));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deserializing crypto list.");
            throw new UserFriendlyException(e.Message);
        }
    }

    // post Alchemy cryptoList
    public async Task<CommonResponseDto<AlchemyOrderQuoteDataDto>> GetAlchemyOrderQuoteAsync(
        GetAlchemyOrderQuoteDto input)
    {
        try
        {
            var key = string.Join(CommonConstant.Dot, input.Crypto, input.Network, input.Fiat, input.Country);
            var orderQuote = await GetBuyOrderQuoteAsync(key, input);
            return new CommonResponseDto<AlchemyOrderQuoteDataDto>(orderQuote);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "GetAlchemyOrderQuoteAsync error");
            return new CommonResponseDto<AlchemyOrderQuoteDataDto>().Error(e, "Internal error please try again later.");
        }
    }

    private async Task<AlchemyOrderQuoteDataDto> GetBuyOrderQuoteAsync(string key, GetAlchemyOrderQuoteDto input)
    {
        var quoteData = await _orderQuoteCache.GetAsync(key);
        if (quoteData == null)
        {
            return await GetOrderQuoteAsync(key, input);
        }

        var fiatListData = await GetAlchemyFiatListWithCacheAsync(new GetAlchemyFiatListDto { Type = input.Side });
        AssertHelper.NotEmpty(fiatListData, "Query Alchemy fiat list empty");
        
        var fiat = fiatListData.FirstOrDefault(t => t.Currency == input.Fiat);
        AssertHelper.NotEmpty(fiatListData, "{Fiat} not found in Alchemy fiat list", input.Fiat);

        var amount = input.Amount.SafeToDouble();
        var fixedFee = fiat.FixedFee.SafeToDouble();
        var feeRate = fiat.FeeRate.SafeToDouble();
        var networkFee = quoteData.NetworkFee.SafeToDouble();
        var cryptoPrice = quoteData.CryptoPrice.SafeToDouble();
        var rampFee = fixedFee + amount * feeRate;
        quoteData.RampFee = rampFee.ToString("f2");
        quoteData.CryptoQuantity = ((amount - rampFee - networkFee) / cryptoPrice).ToString("f8");

        return quoteData;
    }

    private async Task<AlchemyOrderQuoteDataDto> GetOrderQuoteAsync(string key, GetAlchemyOrderQuoteDto input)
    {
        return await _orderQuoteCache.GetOrAddAsync(
            key,
            async () => await _alchemyProvider.GetAlchemyOrderQuote(input),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_alchemyOptions.OrderQuoteExpirationMinutes)
            }
        );
    }


    // generate alchemy signature
    public async Task<CommonResponseDto<AlchemySignatureResultDto>> GetAlchemySignatureAsync(
        GetAlchemySignatureDto input)
    {
        try
        {
            return new CommonResponseDto<AlchemySignatureResultDto>(new AlchemySignatureResultDto()
            {
                Signature = AlchemyHelper.AesEncrypt($"address={input.Address}&appId={_alchemyOptions.AppId}",
                    _alchemyOptions.AppSecret)
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Alchemy signature AES encrypting exception");
            return new CommonResponseDto<AlchemySignatureResultDto>().Error(e,
                $"Error AES encrypting, error msg is {e.Message}");
        }
    }

    public Task<AlchemyBaseResponseDto<string>> GetAlchemyApiSignatureAsync(Dictionary<string, string> input)
    {
        try
        {
            // Ensure input isn't fake webhook data.
            AssertHelper.IsTrue(!input.ContainsKey("status"), "invalid param keys");
            AssertHelper.IsTrue(input.TryGetValue("appId", out var appId), "appId missing");
            var appSecret = _alchemyOptions.NftAppId == appId
                ? _alchemyOptions.NftAppSecret
                : _alchemyOptions.AppSecret;

            var src = ThirdPartHelper.ConvertObjectToSortedString(input, AlchemyHelper.SignatureField);
            var sign = AlchemyHelper.HmacSign(src, appSecret);
            _logger.LogInformation("GetAlchemyApiSignatureAsync, sourceStr={Source}, signature={Sign}", src, sign);
            return Task.FromResult(new AlchemyBaseResponseDto<string>(sign));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetAlchemyApiSignatureAsync error");
            return Task.FromResult(AlchemyBaseResponseDto<string>.Fail(e.Message));
        }
    }
}