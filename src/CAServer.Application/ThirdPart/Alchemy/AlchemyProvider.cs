using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CAServer.ThirdPart.Provider;

public interface IAlchemyProvider
{
    Task<AlchemyFiatListResponseDto> GetAlchemyFiatList(GetAlchemyFiatListDto request);
    Task<QueryAlchemyOrderInfoResponseDto> GetAlchemyOrder(QueryAlchemyOrderDto request);
    Task<AlchemyCryptoListResponseDto> GetAlchemyCryptoList(GetAlchemyCryptoListDto request);
    Task<AlchemyOrderQuoteResponseDto> QueryAlchemyOrderQuoteList(GetAlchemyOrderQuoteDto request);

    Task<string> HttpPost2AlchemyAsync(string path, string inputStr);
}

public static class AlchemyApi
{
    public static ApiInfo GetOrder { get; } = new(HttpMethod.Get, "/merchant/query/trade");
    public static ApiInfo GetFiatList { get; } = new(HttpMethod.Get, "/merchant/fiat/list");
    public static ApiInfo GetCryptoList { get; } = new(HttpMethod.Get, "/merchant/crypto/list");

    public static ApiInfo FetchToken { get; } = new(HttpMethod.Post, "/merchant/getToken");
    public static ApiInfo CreateOrder { get; } = new(HttpMethod.Post, "/merchant/trade/create");
    public static ApiInfo QueryPrice { get; } = new(HttpMethod.Post, "/merchant/order/quote");
}

public class AlchemyProvider : AbstractThirdPartyProvider, IAlchemyProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AlchemyProvider> _logger;
    private readonly AlchemyOptions _alchemyOptions;

    public AlchemyProvider(IHttpClientFactory httpClientFactory,
        IOptions<ThirdPartOptions> merchantOptions,
        ILogger<AlchemyProvider> logger) : base(httpClientFactory, logger)
    {
        _httpClientFactory = httpClientFactory;
        _alchemyOptions = merchantOptions.Value.alchemy;
        _logger = logger;
    }

    private Dictionary<string, string> GetAlchemyRequestHeader()
    {
        var timeStamp = TimeHelper.GetTimeStampInMilliseconds().ToString();
        var sign = GenerateAlchemyApiSign(timeStamp);
        _logger.LogDebug("appId: {AppId}, timeStamp: {TimeStamp}, signature: {Signature}", _alchemyOptions.AppId,
            timeStamp, sign);
        return new Dictionary<string, string>
        {
            ["appId"] = _alchemyOptions.AppId,
            ["timestamp"] = timeStamp,
            ["sign"] = sign
        };
    }

    private Dictionary<string, string> ToParamDict(object input)
    {
        var res = new Dictionary<string, string>();
        foreach (var p in input.GetType().GetProperties())
            res[char.ToLower(p.Name[0]) + p.Name.Substring(1)] = p.GetValue(input)?.ToString();

        return res;
    }

    public async Task<AlchemyFiatListResponseDto> GetAlchemyFiatList(GetAlchemyFiatListDto request)
    {
        return await Invoke<AlchemyFiatListResponseDto>(_alchemyOptions.BaseUrl, AlchemyApi.GetFiatList,
            param: ToParamDict(request),
            header: GetAlchemyRequestHeader());
    }

    public async Task<QueryAlchemyOrderInfoResponseDto> GetAlchemyOrder(QueryAlchemyOrderDto request)
    {
        return await Invoke<QueryAlchemyOrderInfoResponseDto>(_alchemyOptions.BaseUrl, AlchemyApi.GetOrder,
            param: ToParamDict(request),
            header: GetAlchemyRequestHeader());
    }

    public async Task<AlchemyCryptoListResponseDto> GetAlchemyCryptoList(GetAlchemyCryptoListDto request)
    {
        return await Invoke<AlchemyCryptoListResponseDto>(_alchemyOptions.BaseUrl, AlchemyApi.GetCryptoList,
            param: ToParamDict(request),
            header: GetAlchemyRequestHeader());
    }

    public async Task<AlchemyOrderQuoteResponseDto> QueryAlchemyOrderQuoteList(GetAlchemyOrderQuoteDto request)
    {
        return await Invoke<AlchemyOrderQuoteResponseDto>(_alchemyOptions.BaseUrl, AlchemyApi.QueryPrice,
            body: JsonConvert.SerializeObject(request, JsonSettings),
            header: GetAlchemyRequestHeader());
    }

    public async Task<string> HttpPost2AlchemyAsync(string path, string inputStr)
    {
        _logger.LogInformation("[ACH]send request body : \n{requestBody}", inputStr);

        StringContent str2Json = new StringContent(inputStr, Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient();

        SetAlchemyRequestHeader(client);
        HttpResponseMessage respMsg = await client.PostAsync(_alchemyOptions.BaseUrl + path, str2Json);
        var respStr = await respMsg.Content.ReadAsStringAsync();

        _logger.LogInformation("[ACH][{StatusCode}][post]request url: \n{url}, body :{respStr}", respMsg.StatusCode,
            _alchemyOptions.BaseUrl + path, respStr);

        return respStr;
    }

    // Set Alchemy request header with appId timestamp sign.
    private void SetAlchemyRequestHeader(HttpClient client)
    {
        string timeStamp = TimeHelper.GetTimeStampInMilliseconds().ToString();
        var sign = GenerateAlchemyApiSign(timeStamp);
        _logger.LogDebug("appId: {AppId}, timeStamp: {TimeStamp}, signature: {Signature}", _alchemyOptions.AppId,
            timeStamp, sign);

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