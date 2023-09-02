using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Common.Dtos;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart.Alchemy;
using CAServer.ThirdPart.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Provider;


public class AlchemyApi
{
    public static ApiInfo NftResultNotice { get; } = new(HttpMethod.Post, "/nft/openapi/merchant/notice");
    public static ApiInfo QueryNftTrade { get; } = new(HttpMethod.Get, "/nft/openapi/query/trade");
}


public class AlchemyProvider : ISingletonDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AlchemyProvider> _logger;
    private readonly AlchemyOptions _alchemyOptions;
    private readonly IHttpProvider _httpProvider;

    public AlchemyProvider(IHttpClientFactory httpClientFactory,
        IOptions<ThirdPartOptions> merchantOptions,
        ILogger<AlchemyProvider> logger, IHttpProvider httpProvider)
    {
        _httpClientFactory = httpClientFactory;
        _alchemyOptions = merchantOptions.Value.Alchemy;
        _logger = logger;
        _httpProvider = httpProvider;
    }

    [Obsolete("use HttpProvider instead")]
    public async Task<string> HttpGetFromAlchemy(string path)
    {
        var client = _httpClientFactory.CreateClient();
        SetAlchemyRequestHeader(client);
        HttpResponseMessage respMsg = await client.GetAsync(_alchemyOptions.BaseUrl + path);
        var respStr = await respMsg.Content.ReadAsStringAsync();

        _logger.LogInformation("[ACH][{StatusCode}][get]request url: \n{url}", respMsg.StatusCode,
            _alchemyOptions.BaseUrl + path);

        return respStr;
    }
    
    [Obsolete("use HttpProvider instead")]
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


    /// <summary>
    ///     Notice Alchemy NFT release result
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<AlchemyBaseResponseDto> NoticeNftReleaseResult(AlchemyNftReleaseNoticeRequestDto request)
    {
        return await _httpProvider.Invoke<AlchemyNftOrderDto>(_alchemyOptions.NftBaseUrl,
            AlchemyApi.QueryNftTrade,
            header: GetAlchemyRequestHeader(),
            param: new Dictionary<string, string>
            {
                ["orderNo"] = request.OrderNo
            }
        );
    }
    

    /// <summary>
    ///     Notice Alchemy NFT release result
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<AlchemyNftOrderDto> QueryNftTrade(AlchemyNftReleaseNoticeRequestDto request)
    {
        return await _httpProvider.Invoke<AlchemyNftOrderDto>(_alchemyOptions.NftBaseUrl,
            AlchemyApi.NftResultNotice,
            header: GetAlchemyRequestHeader(),
            body: JsonConvert.SerializeObject(request, HttpProvider.DefaultJsonSettings));
    }
    

    // Set Alchemy request header with appId timestamp sign.
    private void SetAlchemyRequestHeader(HttpClient client)
    {
        foreach (var kv in GetAlchemyRequestHeader())
        {   
            client.DefaultRequestHeaders.Add(kv.Key, kv.Value);
        }
    }

    private Dictionary<string,string> GetAlchemyRequestHeader()
    {
        var timeStamp = TimeHelper.GetTimeStampInMilliseconds().ToString();
        var sign = GenerateAlchemyApiSign(timeStamp);
        _logger.LogDebug("appId: {AppId}, timeStamp: {TimeStamp}, signature: {Signature}", _alchemyOptions.AppId,
            timeStamp, sign);
        return new Dictionary<string, string>
        {
            ["appId"] =  _alchemyOptions.AppId,
            ["timestamp"] =  timeStamp,
            ["sign"] =  sign
        };
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