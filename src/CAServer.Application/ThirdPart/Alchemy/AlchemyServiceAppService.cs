using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.ThirdPart.Alchemy;

[RemoteService(false), DisableAuditing]
public class AlchemyServiceAppService : CAServerAppService, IAlchemyServiceAppService
{
    private readonly ILogger<AlchemyServiceAppService> _logger;
    private readonly AlchemyOptions _alchemyOptions;
    private readonly IAlchemyProvider _alchemyProvider;
    private readonly AlchemyHelper _alchemyHelper;

    private readonly JsonSerializerSettings _setting = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public AlchemyServiceAppService(IOptions<ThirdPartOptions> merchantOptions, IAlchemyProvider alchemyProvider,
        AlchemyHelper alchemyHelper,
        ILogger<AlchemyServiceAppService> logger)
    {
        _alchemyOptions = merchantOptions.Value.alchemy;
        _alchemyProvider = alchemyProvider;
        _alchemyHelper = alchemyHelper;
        _logger = logger;
    }

    // get Alchemy login free token
    public async Task<AlchemyTokenDto> GetAlchemyFreeLoginTokenAsync(GetAlchemyFreeLoginTokenDto input)
    {
        return JsonConvert.DeserializeObject<AlchemyTokenDto>(await _alchemyProvider.HttpPost2Alchemy(
            _alchemyOptions.GetTokenUri,
            JsonConvert.SerializeObject(input, Formatting.None, _setting)));
    }

    // get Alchemy fiat list
    public async Task<AlchemyFiatListDto> GetAlchemyFiatListAsync()
    {
        return JsonConvert.DeserializeObject<AlchemyFiatListDto>(
            await _alchemyProvider.HttpGetFromAlchemy(_alchemyOptions.FiatListUri));
    }

    // get Alchemy cryptoList 
    public async Task<AlchemyCryptoListDto> GetAlchemyCryptoListAsync(GetAlchemyCryptoListDto input)
    {
        string queryString = string.Join("&",
            input.GetType().GetProperties()
                .Select(p => $"{char.ToLower(p.Name[0]) + p.Name.Substring(1)}={p.GetValue(input)}"));

        return JsonConvert.DeserializeObject<AlchemyCryptoListDto>(
            await _alchemyProvider.HttpGetFromAlchemy(_alchemyOptions.CryptoListUri + "?" + queryString));
    }

    // post Alchemy cryptoList
    public async Task<AlchemyOrderQuoteResultDto> GetAlchemyOrderQuoteAsync(GetAlchemyOrderQuoteDto input)
    {
        return JsonConvert.DeserializeObject<AlchemyOrderQuoteResultDto>(await _alchemyProvider.HttpPost2Alchemy(
            _alchemyOptions.OrderQuoteUri,
            JsonConvert.SerializeObject(input, Formatting.None, _setting)));
    }


    public AlchemySignatureResultDto GetAlchemySignatureV2Async(object input)
    {
        return _alchemyHelper.GetAlchemySignatureAsync(input, _alchemyOptions.AppSecret,
            new List<string>() { "signature" });
    }

    public async Task<AlchemySignatureResultDto> GetAlchemySignatureAsync(GetAlchemySignatureDto input)
    {
        try
        {
            return new AlchemySignatureResultDto()
            {
                Signature = AlchemyHelper.AesEncrypt(
                    $"address={input.Address}&appId={_alchemyOptions.AppId}",
                    _alchemyOptions.AppSecret)
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "AES encrypting exception");
            return new AlchemySignatureResultDto()
            {
                Success = "Fail",
                ReturnMsg = $"Error AES encrypting, error msg is {e.Message}"
            };
        }
    }
}