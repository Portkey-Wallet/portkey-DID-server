using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AlchemyServiceAppService> _logger;
    private readonly AlchemyOptions _alchemyOptions;

    private readonly JsonSerializerSettings _setting = new()
    {
        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
    };

    public AlchemyServiceAppService(IHttpClientFactory httpClientFactory,
        IOptions<ThirdPartOptions> merchantOptions,
        ILogger<AlchemyServiceAppService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _alchemyOptions = merchantOptions.Value.alchemy;
        _logger = logger;
    }

    // get Alchemy login free token
    public async Task<AlchemyTokenDto> GetAlchemyFreeLoginTokenAsync(GetAlchemyFreeLoginTokenDto input)
    {
        return JsonConvert.DeserializeObject<AlchemyTokenDto>(await HttpPost2Alchemy("/merchant/getToken",
            JsonConvert.SerializeObject(input, Formatting.None, _setting)));
    }

    // get Alchemy fiat list
    public async Task<AlchemyFiatListDto> GetAlchemyFiatListAsync()
    {
        return JsonConvert.DeserializeObject<AlchemyFiatListDto>(await HttpGetFromAlchemy("/merchant/fiat/list"));
    }

    // get Alchemy cryptoList 
    public async Task<AlchemyCryptoListDto> GetAlchemyCryptoListAsync(GetAlchemyCryptoListDto input)
    {
        string queryString = string.Join("&",
            input.GetType().GetProperties()
                .Select(p => $"{char.ToLower(p.Name[0]) + p.Name.Substring(1)}={p.GetValue(input)}"));

        return JsonConvert.DeserializeObject<AlchemyCryptoListDto>(
            await HttpGetFromAlchemy("/merchant/crypto/list" + "?" + queryString));
    }

    // post Alchemy cryptoList
    public async Task<AlchemyOrderQuoteResultDto> GetAlchemyOrderQuoteAsync(GetAlchemyOrderQuoteDto input)
    {
        return JsonConvert.DeserializeObject<AlchemyOrderQuoteResultDto>(await HttpPost2Alchemy("/merchant/order/quote",
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
            _logger.LogError($"AES encrypting exception , error msg is {e}");
            return new AlchemySignatureResultDto()
            {
                Success = "Fail",
                ReturnMsg = $"Error AES encrypting, error msg is {e}"
            };
        }
    }


    private async Task<string> HttpGetFromAlchemy(string path)
    {
        var client = _httpClientFactory.CreateClient();
        SetAlchemyRequestHeader(client);
        HttpResponseMessage respMsg = await client.GetAsync(_alchemyOptions.BaseUrl + path);
        var respStr = await respMsg.Content.ReadAsStringAsync();

        _logger.Debug($"[ACH][get]request url: \n{_alchemyOptions.BaseUrl + path}");

        return respStr;
    }

    private async Task<string> HttpPost2Alchemy(string path, string inputStr)
    {
        _logger.Debug($"[ACH]send request input str : \n{inputStr}");

        StringContent str2Json = new StringContent(inputStr, Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient();
        SetAlchemyRequestHeader(client);
        HttpResponseMessage respMsg = await client.PostAsync(_alchemyOptions.BaseUrl + path, str2Json);
        var respStr = await respMsg.Content.ReadAsStringAsync();

        _logger.Debug($"[ACH][post]request url: \n{_alchemyOptions.BaseUrl + path + str2Json}");

        return respStr;
    }

    // Set Alchemy request header with appId timestamp sign.
    private void SetAlchemyRequestHeader(HttpClient client)
    {
        string timeStamp = TimeStampHelper.GetTimeStampInMilliseconds();
        var sign = GenerateAlchemyApiSign(timeStamp);
        _logger.Debug($"appId: {_alchemyOptions.AppId}, timeStamp: {timeStamp}, sign: {sign}");

        client.DefaultRequestHeaders.Add("appId", _alchemyOptions.AppId);
        client.DefaultRequestHeaders.Add("timestamp", timeStamp);
        client.DefaultRequestHeaders.Add("sign", sign);
    }

    // Generate Alchemy request sigh by "appId + appSecret + timestamp".
    private string GenerateAlchemyApiSign(string timeStamp)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(_alchemyOptions.AppId + _alchemyOptions.AppSecret + timeStamp);
        byte[] hashBytes = SHA1.Create().ComputeHash(bytes);

        StringBuilder sb = new StringBuilder();
        foreach (var t in hashBytes)
        {
            sb.Append(t.ToString("X2"));
        }

        return sb.ToString().ToLower();
    }
}