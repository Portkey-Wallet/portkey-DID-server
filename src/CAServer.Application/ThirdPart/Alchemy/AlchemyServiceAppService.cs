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
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Alchemy;

[RemoteService(false), DisableAuditing]
public partial class AlchemyServiceAppService : CAServerAppService, IThirdPartAppService, IAlchemyServiceAppService
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
    public async Task<AlchemyTokenResponseDto> GetAlchemyFreeLoginTokenAsync(GetAlchemyFreeLoginTokenDto input)
    {
        try
        {
            return JsonConvert.DeserializeObject<AlchemyTokenResponseDto>(await _alchemyProvider.HttpPost2AlchemyAsync(
                _alchemyOptions.GetTokenUri, JsonConvert.SerializeObject(input, Formatting.None, _setting)));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deserializing free login");
            throw new UserFriendlyException(e.Message);
        }
    }

    private async Task<List<AlchemyFiatDto>> GetAlchemyFiatListWithCacheAsync(string key, GetAlchemyFiatListDto input)
    {
        return await _fiatListCache.GetOrAddAsync(
            key,
            async () => (await _alchemyProvider.GetAlchemyFiatList(input)).Data,
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_alchemyOptions.FiatListExpirationMinutes)
            }
        );
    }
    
    // generate alchemy signature
    public async Task<AlchemySignatureResponseDto> GetAlchemySignatureAsync(GetAlchemySignatureDto input)
    {
        try
        {
            return new AlchemySignatureResponseDto()
            {
                Signature = AlchemyHelper.AesEncrypt($"address={input.Address}&appId={_alchemyOptions.AppId}",
                    _alchemyOptions.AppSecret)
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Alchemy signature AES encrypting exception");
            return new AlchemySignatureResponseDto()
            {
                Success = "Fail",
                ReturnMsg = $"Error AES encrypting, error msg is {e.Message}"
            };
        }
    }

}