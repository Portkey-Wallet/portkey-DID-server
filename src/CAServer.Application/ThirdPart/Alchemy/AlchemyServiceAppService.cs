using System;
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

    private readonly JsonSerializerSettings _setting = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public AlchemyServiceAppService(IOptions<ThirdPartOptions> merchantOptions, IAlchemyProvider alchemyProvider,
        ILogger<AlchemyServiceAppService> logger)
    {
        _alchemyOptions = merchantOptions.Value.alchemy;
        _alchemyProvider = alchemyProvider;
        _logger = logger;
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
            string queryString = string.Join("&", input.GetType().GetProperties()
                .Select(p => $"{char.ToLower(p.Name[0]) + p.Name.Substring(1)}={p.GetValue(input)}"));

            return JsonConvert.DeserializeObject<AlchemyFiatListDto>(
                await _alchemyProvider.HttpGetFromAlchemy(_alchemyOptions.FiatListUri + "?" + queryString));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deserializing fiat list.");
            throw new UserFriendlyException(e.Message);
        }
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
            return JsonConvert.DeserializeObject<AlchemyOrderQuoteResultDto>(
                await _alchemyProvider.HttpPost2AlchemyAsync(_alchemyOptions.OrderQuoteUri,
                    JsonConvert.SerializeObject(input, Formatting.None, _setting)));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deserializing order quote.");
            throw new UserFriendlyException(e.Message);
        }
    }

    // generate alchemy
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
            _logger.LogError(e, "AES encrypting exception");
            return new AlchemySignatureResultDto()
            {
                Success = "Fail",
                ReturnMsg = $"Error AES encrypting, error msg is {e.Message}"
            };
        }
    }
}