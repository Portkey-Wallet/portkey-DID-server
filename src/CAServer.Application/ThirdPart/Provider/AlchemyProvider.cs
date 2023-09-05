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
using CAServer.ThirdPart.Dtos;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Provider;

public static class AlchemyApi
{
    public static ApiInfo NftResultNotice { get; } = new(HttpMethod.Post, "/nft/openapi/merchant/notice");
    public static ApiInfo QueryNftTrade { get; } = new(HttpMethod.Get, "/nft/openapi/query/trade");
    public static ApiInfo QueryNftFiatList { get; } = new(HttpMethod.Get, "/nft/openapi/fiat/list");
    
    
    public static ApiInfo QueryFiatList { get; } = new(HttpMethod.Get, "/merchant/fiat/list");
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
    public async Task<AlchemyNftOrderDto> QueryNftTrade(AlchemyNftReleaseNoticeRequestDto request)
    {
        var result = await _httpProvider.Invoke<AlchemyBaseResponseDto<AlchemyNftOrderDto>>(_alchemyOptions.NftBaseUrl,
            AlchemyApi.QueryNftTrade,
            header: GetNftAlchemyRequestHeader(),
            param: new Dictionary<string, string>
            {
                ["orderNo"] = request.OrderNo
            }
        );
        AssertHelper.IsTrue(result.ReturnCode == AlchemyBaseResponseDto<Empty>.SuccessCode,
            "Query Alchemy NFT trade fail ({Code}){Msg}", result.ReturnCode, result.ReturnMsg);
        return result.Data;
    }


    /// <summary>
    ///     Notice Alchemy NFT release result
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task NoticeNftReleaseResult(AlchemyNftReleaseNoticeRequestDto request)
    {
        var res = await _httpProvider.Invoke<AlchemyBaseResponseDto<Empty>>(_alchemyOptions.NftBaseUrl,
            AlchemyApi.NftResultNotice,
            header: GetNftAlchemyRequestHeader(),
            body: JsonConvert.SerializeObject(request, HttpProvider.DefaultJsonSettings));
        AssertHelper.IsTrue(res.ReturnCode == AlchemyBaseResponseDto<Empty>.SuccessCode,
            JsonConvert.SerializeObject(res));
    }


    /// <summary>
    ///     Notice Alchemy NFT release result
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<List<AlchemyFiatDto>> GetNftFiatList()
    {
        var res = await _httpProvider.Invoke<AlchemyBaseResponseDto<List<AlchemyFiatDto>>>(_alchemyOptions.NftBaseUrl,
            AlchemyApi.QueryNftFiatList,
            header: GetNftAlchemyRequestHeader()
        );
        AssertHelper.IsTrue(res.ReturnCode == AlchemyBaseResponseDto<Empty>.SuccessCode,
            JsonConvert.SerializeObject(res));
        return res.Data;
    }

    /// <summary>
    ///     Notice Alchemy NFT release result
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<List<AlchemyFiatDto>> GetFiatList(string type)
    {
        var res = await _httpProvider.Invoke<AlchemyBaseResponseDto<List<AlchemyFiatDto>>>(_alchemyOptions.BaseUrl,
            AlchemyApi.QueryFiatList,
            header: GetRampAlchemyRequestHeader(),
            param: new Dictionary<string, string>{ ["type"] = type }
        );
        AssertHelper.IsTrue(res.ReturnCode == AlchemyBaseResponseDto<Empty>.SuccessCode,
            JsonConvert.SerializeObject(res));
        return res.Data;
    }


    // Set Alchemy request header with appId timestamp sign.
    private void SetAlchemyRequestHeader(HttpClient client)
    {
        foreach (var kv in GetRampAlchemyRequestHeader())
        {
            client.DefaultRequestHeaders.Add(kv.Key, kv.Value);
        }
    }

    private Dictionary<string, string> GetRampAlchemyRequestHeader()
    {
        return GetAlchemyRequestHeader(_alchemyOptions.AppId, _alchemyOptions.AppSecret);
    }


    private Dictionary<string, string> GetNftAlchemyRequestHeader()
    {
        return GetAlchemyRequestHeader(_alchemyOptions.NftAppId, _alchemyOptions.NftAppSecret);
    }


    private Dictionary<string, string> GetAlchemyRequestHeader(string appId, string appSecret)
    {
        var timeStamp = TimeHelper.GetTimeStampInMilliseconds().ToString();
        var sign = AlchemyHelper.GenerateAlchemyApiSign(appId, appSecret, timeStamp);
        _logger.LogDebug("appId: {AppId}, timeStamp: {TimeStamp}, signature: {Signature}", _alchemyOptions.AppId,
            timeStamp, sign);
        return new Dictionary<string, string>
        {
            ["appId"] = _alchemyOptions.AppId,
            ["timestamp"] = timeStamp,
            ["sign"] = sign
        };
    }

}