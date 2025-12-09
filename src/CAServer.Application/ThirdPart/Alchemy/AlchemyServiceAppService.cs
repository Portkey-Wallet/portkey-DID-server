using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains;
using CAServer.Options;
using CAServer.SecurityServer;
using CAServer.Signature.Provider;
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
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly IOptionsMonitor<RampOptions> _rampOptions;
    private readonly AlchemyProvider _alchemyProvider;
    private readonly IDistributedCache<List<AlchemyFiatDto>> _fiatListCache;
    private readonly IDistributedCache<List<AlchemyCryptoDto>> _cryptoListCache;
    private readonly IDistributedCache<List<AlchemyFiatDto>> _nftFiatListCache;
    private readonly IDistributedCache<AlchemyOrderQuoteDataDto> _orderQuoteCache;
    private readonly ISecretProvider _secretProvider;

    private readonly JsonSerializerSettings _setting = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public AlchemyServiceAppService(IOptionsMonitor<ThirdPartOptions> thirdPartOptions, AlchemyProvider alchemyProvider,
        ILogger<AlchemyServiceAppService> logger, IDistributedCache<List<AlchemyFiatDto>> fiatListCache,
        IDistributedCache<AlchemyOrderQuoteDataDto> orderQuoteCache,
        IDistributedCache<List<AlchemyFiatDto>> nftFiatListCache,
        IDistributedCache<List<AlchemyCryptoDto>> cryptoListCache, IOptionsMonitor<RampOptions> rampOptions,
        ISecretProvider secretProvider)
    {
        _thirdPartOptions = thirdPartOptions;
        _alchemyProvider = alchemyProvider;
        _logger = logger;
        _fiatListCache = fiatListCache;
        _orderQuoteCache = orderQuoteCache;
        _nftFiatListCache = nftFiatListCache;
        _cryptoListCache = cryptoListCache;
        _rampOptions = rampOptions;
        _secretProvider = secretProvider;
    }

    private AlchemyOptions AlchemyOptions()
    {
        return _thirdPartOptions.CurrentValue.Alchemy;
    }

    private ThirdPartProvider AlchemyRampOptions()
    {
        var exists =
            _rampOptions.CurrentValue.Providers.TryGetValue(ThirdPartNameType.Alchemy.ToString(),
                out var achRampOptions);
        return exists ? achRampOptions : null;
    }

    /// get Alchemy login free token
    public async Task<CommonResponseDto<AlchemyTokenDataDto>> GetAlchemyFreeLoginTokenAsync(
        GetAlchemyFreeLoginTokenDto input)
    {
        try
        {
            return new CommonResponseDto<AlchemyTokenDataDto>(
                await _alchemyProvider.GetAlchemyRampFreeLoginTokenAsync(input));
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
            var resp = await _alchemyProvider.GetNftFreeLoginTokenAsync(input);
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
                async () => await _alchemyProvider.GetAlchemyFiatListAsync(input),
                () => new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(AlchemyOptions().FiatListExpirationMinutes)
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

    public async Task<(List<AlchemyFiatDto>, string)> GetAlchemyFiatListAsync(GetAlchemyFiatListDto input)
    {
        try
        {
            return (await _alchemyProvider.GetAlchemyFiatListAsync(input), "succeed");
        }
        catch (Exception e)
        {
            _logger.LogInformation("GetAlchemyFiatListAsync Error:{0}", e.Message);
            return (new List<AlchemyFiatDto>(), e.Message);
        }
    }
    
    /// NFT FiatList
    public async Task<List<AlchemyFiatDto>> GetAlchemyNftFiatListAsync()
    {
        return await _nftFiatListCache.GetOrAddAsync(CommonConstant.NftFiatListKey,
            async () => await _alchemyProvider.GetNftFiatListAsync(),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(AlchemyOptions().NftFiatListExpirationMinutes)
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
                async () => await _alchemyProvider.GetAlchemyCryptoListAsync(input),
                () => new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(AlchemyOptions().CryptoListExpirationMinutes)
                }
            );

            var r =  await _alchemyProvider.GetAlchemyCryptoListAsync(input);
            _logger.LogDebug("GetAlchemyCryptoListAsync input = {0} resp = {1}",JsonConvert.SerializeObject(input), JsonConvert.SerializeObject(r));
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
            var cryptoList = await GetAlchemyCryptoListAsync(new GetAlchemyCryptoListDto
            {
                Fiat = input.Fiat
            });
            AssertHelper.IsTrue(cryptoList.Success, "Query Alchemy crypto list fail");
            AssertHelper.NotEmpty(cryptoList.Data, "Empty Alchemy crypto list");
            var mappingNetworkExists =
                AlchemyRampOptions().NetworkMapping.TryGetValue(input.Network, out var mappingNetwork);
            var cryptoItem = cryptoList.Data
                .Where(c => c.Network == (mappingNetworkExists ? mappingNetwork : input.Network))
                .Where(c => c.Crypto == input.Crypto)
                .FirstOrDefault(c => input.IsBuy() ? c.BuyEnable.SafeToInt() > 0 : c.SellEnable.SafeToInt() > 0);
            AssertHelper.NotNull(cryptoItem, "Crypto {Crypto} not found in Alchemy list.", input.Crypto);

            input.Network = cryptoItem.Network;
            var quoteData = await GetOrderQuoteWithCacheAsync(input);
            quoteData.Network = cryptoItem.Network;
            AssertHelper.NotNull(quoteData, "Cached order quote empty");

            var fiatListData = await GetAlchemyFiatListWithCacheAsync(new GetAlchemyFiatListDto { Type = input.Side });
            AssertHelper.IsTrue(fiatListData.Success, "Query Alchemy fiat list failed, {Msg}", fiatListData.Message);

            var fiat = fiatListData.Data.FirstOrDefault(t => t.Currency == input.Fiat && t.Country == input.Country);
            AssertHelper.NotNull(fiat, "{Fiat} not found in Alchemy fiat list", input.Fiat);

            // var fixedFee = fiat.FixedFee.SafeToDecimal();
            
            // Exchange of [ fiat : crypto ]
            var cryptoNetworkFee = quoteData.CryptoNetworkFee.SafeToDecimal();
            var networkFee = cryptoNetworkFee > 0 ? 0 : quoteData.NetworkFee.SafeToDecimal();
            var cryptoPrice = quoteData.CryptoPrice.SafeToDecimal();
            var rampFee = quoteData.RampFee.SafeToDecimal();
            
            var inputAmount = input.Amount.SafeToDecimal();
            var fiatAmount = input.IsBuy() ? inputAmount : inputAmount * cryptoPrice;
            
            /*
             * on-ramp: input-amount is FiatQuantity, which user will pay
             * off-ramp: FiatQuantity = (CryptoQuantity * fiat-crypto-exchange) - fee
             */
            quoteData.FiatQuantity = input.IsBuy()
                ? input.Amount
                : (fiatAmount - rampFee - networkFee).ToString(CultureInfo.InvariantCulture);
            quoteData.RampFee = rampFee.ToString("f2");

            /*
             * on-ramp: CryptoQuantity = (FiatQuantity - Fee ) / fiat-crypto-exchange
             * off-ramp: input-amount is CryptoQuantity, which user will pay
             */
            quoteData.CryptoQuantity = input.IsBuy()
                ? ((fiatAmount - rampFee - networkFee) / cryptoPrice - cryptoNetworkFee).ToString(CultureInfo.InvariantCulture)
                : input.Amount;
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
        // cache with amount as int value 
        var cacheKey =
            GrainIdHelper.GenerateGrainId(PriceCacheKey, input.Side, input.Crypto, input.Network, input.Fiat,
                input.Country, input.Amount.SafeToInt());
        return await _orderQuoteCache.GetOrAddAsync(cacheKey,
            async () => await _alchemyProvider.GetAlchemyOrderQuoteAsync(input),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(AlchemyOptions().OrderQuoteExpirationMinutes)
            }
        );
    }

    /// generate Alchemy signature
    public async Task<CommonResponseDto<AlchemySignatureResultDto>> GetAlchemySignatureAsync(
        GetAlchemySignatureDto input)
    {
        try
        {
            var sign = await _secretProvider.GetAlchemyAesSignAsync(AlchemyOptions().AppId,
                $"address={input.Address}&appId={AlchemyOptions().AppId}");
            return new CommonResponseDto<AlchemySignatureResultDto>(new AlchemySignatureResultDto
            {
                Signature = sign
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
    public async Task<AlchemyBaseResponseDto<string>> GetAlchemyApiSignatureAsync(Dictionary<string, string> input)
    {
        try
        {
            // Ensure input isn't fake webhook data.
            AssertHelper.IsTrue(!input.ContainsKey("status"), "invalid param keys");
            AssertHelper.IsTrue(input.TryGetValue("appId", out var appId), "appId missing");
            var src = ThirdPartHelper.ConvertObjectToSortedString(input, AlchemyHelper.SignatureField);
            var sign = await _secretProvider.GetAlchemyHmacSignAsync(appId, src);
            _logger.LogInformation("GetAlchemyApiSignatureAsync, sourceStr={Source}, signature={Sign}", src, sign);
            return new AlchemyBaseResponseDto<string>(sign);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetAlchemyApiSignatureAsync error");
            return AlchemyBaseResponseDto<string>.Fail(e.Message);
        }
    }
}