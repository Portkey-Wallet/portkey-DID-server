using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.Auditing;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

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
            return new AlchemySignatureResultDto()
            {
                Signature = AlchemyHelper.AESEncrypt(
                    $"address={input.Address}&appId={_alchemyOptions.AppId}",
                    _alchemyOptions.AppSecret)
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

    // doc: https://alchemypay.readme.io/docs/api-sign
    public async Task<AlchemySignatureResultDto> GetAlchemySignatureV2Async(object input, List<string> ignoreProperties)
    {
        try
        {
            var signParamDictionary = ConvertObjectToDictionary(input);
            
            // ignore some key such as "signature" properties
            foreach (var key in ignoreProperties?? new List<string>())
            {
                signParamDictionary.Remove(key);
            }
            
            var sortedParams = signParamDictionary.OrderBy(d => d.Key, StringComparer.Ordinal);
            var signSource = string.Join("&", sortedParams.Select(kv => $"{kv.Key}={kv.Value}"));
            _logger.Debug("[ACH] signSource = {signSource}", signSource);
            return new AlchemySignatureResultDto()
            {
                Signature = AlchemyHelper.ComputeHmacsha256(signSource, _alchemyOptions.AppSecret)
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

    public static Dictionary<string, string> ConvertObjectToDictionary(object obj)
    {
        if (obj == null)
        {
            return new Dictionary<string, string>();
        }

        Dictionary<string, string> dict = new Dictionary<string, string>();
        Guid emptyGuid = new Guid();

        // If the object is a dictionary, handle it separately
        if (obj is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                dict.Add(entry.Key.ToString() ?? string.Empty, entry.Value?.ToString());
            }

            return dict;
        }

        // If not, process each property
        foreach (PropertyInfo property in obj.GetType().GetProperties())
        {
            // Skip indexed properties
            if (property.GetIndexParameters().Length != 0)
            {
                continue;
            }

            if (property.PropertyType == typeof(string) || property.PropertyType.IsValueType)
            {
                object value = property.GetValue(obj);

                // Skip null value or empty Guid value
                if (value == null || property.PropertyType == typeof(Guid) && value.Equals(emptyGuid))
                {
                    continue;
                }

                // convert first char to lower case 
                dict.Add(property.Name.Substring(0, 1).ToLowerInvariant() + property.Name.Substring(1),
                    value.ToString());
            }
        }

        return dict;
    }
}