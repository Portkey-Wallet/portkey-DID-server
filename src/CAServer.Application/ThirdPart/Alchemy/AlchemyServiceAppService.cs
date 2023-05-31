using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.Auditing;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace CAServer.ThirdPart.Alchemy;

[RemoteService(false), DisableAuditing]
public class AlchemyServiceAppService : CAServerAppService, IAlchemyServiceAppService
{
    private readonly ILogger<AlchemyServiceAppService> _logger;
    private readonly AlchemyOptions _alchemyOptions;
    private readonly IAlchemyProvider _alchemyProvider;

    private readonly JsonSerializerSettings _setting = new()
    {
        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
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

    public async Task<AlchemySignatureResultDto> GetAlchemySignatureAsync(GetAlchemySignatureDto input)
    {
        try
        {
            if (input.SignParams.IsNullOrEmpty())
            {
                if (input.Address.IsNullOrEmpty())
                {
                    throw new UserFriendlyException("require sign param");
                }

                // old version, sign only by address & appId
                return new AlchemySignatureResultDto()
                {
                    Signature = AlchemyHelper.AESEncrypt(
                        $"address={input.Address}&appId={_alchemyOptions.AppId}",
                        _alchemyOptions.AppSecret)
                };
            }

            // doc: https://alchemypay.readme.io/docs/api-sign
            var sortedParams = input.SignParams.OrderBy(d => d.Key, StringComparer.Ordinal);
            var signSource = string.Join("&", sortedParams.Select(kv => $"{kv.Key}={kv.Value}"));
            _logger.Debug("[ACH] address={address}, signSource = {signSource}", input.Address, signSource);
            return new AlchemySignatureResultDto() 
            {
                Signature = AlchemyHelper.AESEncrypt(signSource, _alchemyOptions.AppSecret)
            };
        }
        catch (Exception e)
        {
            _logger.LogError("AES encrypting exception , error msg is {errorMsg}", e.Message);
            return new AlchemySignatureResultDto()
            {
                Success = "Fail",
                ReturnMsg = $"Error AES encrypting, error msg is {e.Message}"
            };
        }
    }
}