using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CAServer.Alchemy.Dtos;
using CAServer.Commons;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace CAServer.Alchemy;

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
        IOptions<AlchemyOptions> alchemyOption,
        ILogger<AlchemyServiceAppService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _alchemyOptions = alchemyOption.Value;
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

    private async Task<string> HttpGetFromAlchemy(string path)
    {
        var client = _httpClientFactory.CreateClient();
        SetAlchemyRequestHeader(client);
        HttpResponseMessage respMsg = await client.GetAsync(_alchemyOptions.BaseUrl + path);
        var respStr = await respMsg.Content.ReadAsStringAsync();

        // _logger.Debug($"[ACH][get]receive response json body: \n{respStr}");
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

        // _logger.Debug($"[ACH][post]receive response json body: \n{respStr}");
        _logger.Debug($"[ACH][post]request url: \n{_alchemyOptions.BaseUrl + path}");
        return respStr;
    }

    // Set Alchemy request header with appId timestamp sign.
    private void SetAlchemyRequestHeader(HttpClient client)
    {
        string timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
        var sign = GenerateAlchemySign(timeStamp);
        _logger.Debug($"appId: {_alchemyOptions.AppId}, timeStamp: {timeStamp}, sign: {sign}");

        client.DefaultRequestHeaders.Add("appId", _alchemyOptions.AppId);
        client.DefaultRequestHeaders.Add("timestamp", timeStamp);
        client.DefaultRequestHeaders.Add("sign", sign);
    }

    // Generate Alchemy request sigh by "appId + appSecret + timestamp".
    private string GenerateAlchemySign(string timeStamp)
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