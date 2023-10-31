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
    private const string FiatCacheKey = "ramp:achCache:fiat";
    private const string CryptoCacheKey = "ramp:achCache:crypto";
    private const string PriceCacheKey = "ramp:achCache:price";

    private readonly ILogger<AlchemyServiceAppService> _logger;
    private readonly AlchemyOptions _alchemyOptions;
    private readonly AlchemyProvider _alchemyProvider;
    private readonly IDistributedCache<List<AlchemyFiatDto>> _fiatListCache;
    private readonly IDistributedCache<List<AlchemyCryptoDto>> _cryptoListCache;
    private readonly IDistributedCache<List<AlchemyFiatDto>> _nftFiatListCache;
    private readonly IDistributedCache<AlchemyOrderQuoteDataDto> _orderQuoteCache;

    private readonly JsonSerializerSettings _setting = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public AlchemyServiceAppService(IOptions<ThirdPartOptions> thirdPartOptions, AlchemyProvider alchemyProvider,
        ILogger<AlchemyServiceAppService> logger, IDistributedCache<List<AlchemyFiatDto>> fiatListCache,
        IDistributedCache<AlchemyOrderQuoteDataDto> orderQuoteCache,
        IDistributedCache<List<AlchemyFiatDto>> nftFiatListCache,
        IDistributedCache<List<AlchemyCryptoDto>> cryptoListCache)
    {
        _alchemyOptions = thirdPartOptions.Value.Alchemy;
        _alchemyProvider = alchemyProvider;
        _logger = logger;
        _fiatListCache = fiatListCache;
        _orderQuoteCache = orderQuoteCache;
        _nftFiatListCache = nftFiatListCache;
        _cryptoListCache = cryptoListCache;
    }

    /// get Alchemy login free token
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

    /// NFT free login token
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

    /// get Alchemy fiat list
    public async Task<CommonResponseDto<List<AlchemyFiatDto>>> GetAlchemyFiatListWithCacheAsync(
        GetAlchemyFiatListDto input)
    {
        try
        {
            var cacheKey = GrainIdHelper.GenerateGrainId(FiatCacheKey, input.Type);
            var resp = await _fiatListCache.GetOrAddAsync(cacheKey,
                async () => await _alchemyProvider.GetAlchemyFiatList(input),
                () => new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_alchemyOptions.FiatListExpirationMinutes)
                }
            );
            return new CommonResponseDto<List<AlchemyFiatDto>>(resp);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deserializing fiat list");
            return new CommonResponseDto<List<AlchemyFiatDto>>().Error(e, "Get Alchemy fiat list failed");
        }
    }

    /// NFT FiatList
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

    /// get Alchemy cryptoList 
    public async Task<CommonResponseDto<List<AlchemyCryptoDto>>> GetAlchemyCryptoListAsync(
        GetAlchemyCryptoListDto input)
    {
        try
        {
            var cacheKey = GrainIdHelper.GenerateGrainId(CryptoCacheKey, input.Fiat);
            var resp = await _cryptoListCache.GetOrAddAsync(cacheKey,
                async () => await _alchemyProvider.GetAlchemyCryptoList(input),
                () => new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_alchemyOptions.CryptoListExpirationMinutes)
                }
            );
            return new CommonResponseDto<List<AlchemyCryptoDto>>(resp);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetAlchemyCryptoListAsync error");
            return new CommonResponseDto<List<AlchemyCryptoDto>>().Error(e, "Internal error please try again later");
        }
    }

    /// post Alchemy cryptoList
    public async Task<CommonResponseDto<AlchemyOrderQuoteDataDto>> GetAlchemyOrderQuoteAsync(
        GetAlchemyOrderQuoteDto input)
    {
        try
        {
            var quoteData = await GetOrderQuoteWithCacheAsync(input);
            AssertHelper.NotNull(quoteData, "Cached order quote empty");

            var fiatListData = await GetAlchemyFiatListWithCacheAsync(new GetAlchemyFiatListDto { Type = input.Side });
            AssertHelper.IsTrue(fiatListData.Success, "Query Alchemy fiat list failed, {Msg}", fiatListData.Message);

            var fiat = fiatListData.Data.FirstOrDefault(t => t.Currency == input.Fiat);
            AssertHelper.NotNull(fiat, "{Fiat} not found in Alchemy fiat list", input.Fiat);

            var amount = input.Amount.SafeToDouble();
            var fixedFee = fiat.FixedFee.SafeToDouble();
            var feeRate = fiat.FeeRate.SafeToDouble();
            var networkFee = quoteData.NetworkFee.SafeToDouble();
            var cryptoPrice = quoteData.CryptoPrice.SafeToDouble();
            var rampFee = fixedFee + amount * feeRate;
            quoteData.RampFee = rampFee.ToString("f2");
            quoteData.CryptoQuantity = ((amount - rampFee - networkFee) / cryptoPrice).ToString("f8");
            return new CommonResponseDto<AlchemyOrderQuoteDataDto>(quoteData);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "GetAlchemyOrderQuoteAsync error");
            return new CommonResponseDto<AlchemyOrderQuoteDataDto>().Error(e, "Internal error please try again later.");
        }
    }

    private async Task<AlchemyOrderQuoteDataDto> GetOrderQuoteWithCacheAsync(GetAlchemyOrderQuoteDto input)
    {
        // TODO nzc risk detect
        var cacheKey =
            GrainIdHelper.GenerateGrainId(PriceCacheKey, input.Crypto, input.Network, input.Fiat, input.Country);
        return await _orderQuoteCache.GetOrAddAsync(cacheKey,
            async () => await _alchemyProvider.GetAlchemyOrderQuote(input),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_alchemyOptions.OrderQuoteExpirationMinutes)
            }
        );
    }

    /// generate Alchemy signature
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

    /// generate Alchemy API signature
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