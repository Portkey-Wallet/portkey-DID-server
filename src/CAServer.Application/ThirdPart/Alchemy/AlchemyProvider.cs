using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Common.Dtos;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Alchemy;

public static class AlchemyApi
{
    // nft
    public static ApiInfo GetFreeLoginToken { get; } = new(HttpMethod.Post, "/nft/openapi/merchant/getToken");
    public static ApiInfo NftResultNotice { get; } = new(HttpMethod.Post, "/nft/openapi/merchant/notice");
    
    public static ApiInfo QueryNftTrade { get; } = new(HttpMethod.Get, "/nft/openapi/query/trade");
    public static ApiInfo QueryNftFiatList { get; } = new(HttpMethod.Get, "/nft/openapi/fiat/list");
    
    // ramp
    public static ApiInfo RampOrderQuote { get; } = new(HttpMethod.Post, "/merchant/order/quote");
    public static ApiInfo RampFreeLoginToken { get; } = new(HttpMethod.Post, "/merchant/getToken");
    public static ApiInfo UpdateSellOrder { get; } = new(HttpMethod.Post, "/webhooks/off/merchant");
    
    public static ApiInfo QueryFiatList { get; } = new(HttpMethod.Get, "/merchant/fiat/list");
    public static ApiInfo QueryCryptoList { get; } = new(HttpMethod.Get, "/merchant/crypto/list");
    public static ApiInfo QueryOrderTrade { get; } = new(HttpMethod.Get, "/merchant/query/trade");
}

public class AlchemyProvider
{
    private readonly ILogger<AlchemyProvider> _logger;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly IHttpProvider _httpProvider;

    private static readonly JsonSerializerSettings JsonSerializerSettings = JsonSettingsBuilder.New()
        .IgnoreNullValue()
        .WithCamelCasePropertyNamesResolver()
        .WithAElfTypesConverters()
        .Build(); 

    public AlchemyProvider(
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions,
        IHttpProvider httpProvider, ILogger<AlchemyProvider> logger)
    {
        _thirdPartOptions = thirdPartOptions;
        _httpProvider = httpProvider;
        _logger = logger;
    }

    private AlchemyOptions AlchemyOptions()
    {
        return _thirdPartOptions.CurrentValue.Alchemy;
    }

    /// get Alchemy order quote
    public async Task<AlchemyOrderQuoteDataDto> GetAlchemyOrderQuoteAsync(GetAlchemyOrderQuoteDto input)
    {
        var result = await _httpProvider.InvokeAsync<AlchemyBaseResponseDto<AlchemyOrderQuoteDataDto>>(AlchemyOptions().BaseUrl,
            AlchemyApi.RampOrderQuote,
            header: GetRampAlchemyRequestHeader(),
            body: JsonConvert.SerializeObject(input, JsonSerializerSettings),
            withInfoLog: true
        );
        AssertHelper.IsTrue(result.ReturnCode == AlchemyBaseResponseDto<Empty>.SuccessCode,
            "GetAlchemyOrderQuote fail ({Code}){Msg}", result.ReturnCode, result.ReturnMsg);
        return result.Data;
    }
    
    /// get Alchemy Crypto list
    public async Task<List<AlchemyCryptoDto>> GetAlchemyCryptoListAsync(GetAlchemyCryptoListDto input)
    {
        var result = await _httpProvider.InvokeAsync<AlchemyBaseResponseDto<List<AlchemyCryptoDto>>>(AlchemyOptions().BaseUrl,
            AlchemyApi.QueryCryptoList,
            header: GetRampAlchemyRequestHeader(),
            param: JsonConvert.DeserializeObject<Dictionary<string,string>>(JsonConvert.SerializeObject(input, JsonSerializerSettings)),
            withInfoLog: false
        );
        AssertHelper.IsTrue(result.ReturnCode == AlchemyBaseResponseDto<Empty>.SuccessCode,
            "GetAlchemyCryptoList fail ({Code}){Msg}", result.ReturnCode, result.ReturnMsg);
        return result.Data;
    }
    
    /// get Alchemy fiat list
    public async Task<List<AlchemyFiatDto>> GetAlchemyFiatListAsync(GetAlchemyFiatListDto input)
    {
        var result = await _httpProvider.InvokeAsync<AlchemyBaseResponseDto<List<AlchemyFiatDto>>>(AlchemyOptions().BaseUrl,
            AlchemyApi.QueryFiatList,
            header: GetRampAlchemyRequestHeader(),
            param: JsonConvert.DeserializeObject<Dictionary<string,string>>(JsonConvert.SerializeObject(input, JsonSerializerSettings)),
            withInfoLog: false
        );
        AssertHelper.IsTrue(result.ReturnCode == AlchemyBaseResponseDto<Empty>.SuccessCode,
            "GetAlchemyFiatList fail ({Code}){Msg}", result.ReturnCode, result.ReturnMsg);
        return result.Data.Where(f => f.PayMax.SafeToDecimal() == 0 || f.PayMax.SafeToDecimal() - f.PayMin.SafeToDecimal() > 0).ToList();
    }

    /// query Alchemy order info
    public async Task<QueryAlchemyOrderInfo> QueryAlchemyOrderInfoAsync(QueryAlchemyOrderDto input)
    {
        var result = await _httpProvider.InvokeAsync<AlchemyBaseResponseDto<QueryAlchemyOrderInfo>>(AlchemyOptions().BaseUrl,
            AlchemyApi.QueryOrderTrade,
            header: GetRampAlchemyRequestHeader(),
            param: JsonConvert.DeserializeObject<Dictionary<string,string>>(JsonConvert.SerializeObject(input, JsonSerializerSettings)),
            withInfoLog: true
        );
        AssertHelper.IsTrue(result.ReturnCode == AlchemyBaseResponseDto<Empty>.SuccessCode,
            "QueryAlchemyOrderInfoAsync fail ({Code}){Msg}", result.ReturnCode, result.ReturnMsg);
        return result.Data;
    }
    
    
    /// Get Alchemy ramp free login toke 
    public async Task<AlchemyTokenDataDto> GetAlchemyRampFreeLoginTokenAsync(GetAlchemyFreeLoginTokenDto input)
    {
        
        var result = await _httpProvider.InvokeAsync<AlchemyBaseResponseDto<AlchemyTokenDataDto>>(AlchemyOptions().BaseUrl,
            AlchemyApi.RampFreeLoginToken,
            header: GetRampAlchemyRequestHeader(),
            body: JsonConvert.SerializeObject(input, JsonSerializerSettings)
        );
        AssertHelper.IsTrue(result.ReturnCode == AlchemyBaseResponseDto<Empty>.SuccessCode,
            "GetAlchemyRampFreeLoginToken fail ({Code}){Msg}", result.ReturnCode, result.ReturnMsg);
        return result.Data;
    }

    /// Update off-ramp order TxHash
    public async Task UpdateOffRampOrderAsync(WaitToSendOrderInfoDto input)
    {
        var result = await _httpProvider.InvokeAsync<AlchemyBaseResponseDto<AlchemyNftOrderDto>>(AlchemyOptions().BaseUrl,
            AlchemyApi.UpdateSellOrder,
            header: GetRampAlchemyRequestHeader(),
            body: JsonConvert.SerializeObject(input, JsonSerializerSettings),
            withInfoLog: true
        );
        AssertHelper.IsTrue(result.ReturnCode == AlchemyBaseResponseDto<Empty>.SuccessCode,
            "UpdateSellOrder fail ({Code}){Msg}", result.ReturnCode, result.ReturnMsg);
    }

    /// Query NFT Order
    public async Task<AlchemyNftOrderDto> GetNftTradeAsync(AlchemyNftReleaseNoticeRequestDto request)
    {
        var result = await _httpProvider.InvokeAsync<AlchemyBaseResponseDto<AlchemyNftOrderDto>>(AlchemyOptions().NftBaseUrl,
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

    /// Notice Alchemy NFT release result
    public async Task NoticeNftReleaseResultAsync(AlchemyNftReleaseNoticeRequestDto request)
    {
        var res = await _httpProvider.InvokeAsync<AlchemyBaseResponseDto<Empty>>(AlchemyOptions().NftBaseUrl,
            AlchemyApi.NftResultNotice,
            header: GetNftAlchemyRequestHeader(),
            body: JsonConvert.SerializeObject(request, JsonSerializerSettings), 
            withInfoLog:true);
        AssertHelper.IsTrue(res.ReturnCode == AlchemyBaseResponseDto<Empty>.SuccessCode,
            JsonConvert.SerializeObject(res));
    }


    /// Query NFT Fiat List
    public async Task<List<AlchemyFiatDto>> GetNftFiatListAsync()
    {
        var res = await _httpProvider.InvokeAsync<AlchemyBaseResponseDto<List<AlchemyFiatDto>>>(AlchemyOptions().NftBaseUrl,
            AlchemyApi.QueryNftFiatList,
            header: GetNftAlchemyRequestHeader(),
            withDebugLog: false
        );
        AssertHelper.IsTrue(res.ReturnCode == AlchemyBaseResponseDto<Empty>.SuccessCode,
            JsonConvert.SerializeObject(res));
        return res.Data;
    }

    /// Get Alchemy NFT free login Token
    public async Task<AlchemyTokenDataDto> GetNftFreeLoginTokenAsync(GetAlchemyFreeLoginTokenDto input)
    {
        var res = await _httpProvider.InvokeAsync<AlchemyBaseResponseDto<AlchemyTokenDataDto>>(AlchemyOptions().NftBaseUrl,
            AlchemyApi.GetFreeLoginToken,
            body: JsonConvert.SerializeObject(input, JsonSerializerSettings),
            header: GetNftAlchemyRequestHeader()
        );
        AssertHelper.IsTrue(res.ReturnCode == AlchemyBaseResponseDto<Empty>.SuccessCode,
            JsonConvert.SerializeObject(res));
        return res.Data;
    }

    private Dictionary<string, string> GetRampAlchemyRequestHeader()
    {
        return GetAlchemyRequestHeader(AlchemyOptions().AppId, AlchemyOptions().AppSecret);
    }


    private Dictionary<string, string> GetNftAlchemyRequestHeader()
    {
        return GetAlchemyRequestHeader(AlchemyOptions().NftAppId, AlchemyOptions().NftAppSecret);
    }


    private Dictionary<string, string> GetAlchemyRequestHeader(string appId, string appSecret)
    {
        var timeStamp = TimeHelper.GetTimeStampInMilliseconds().ToString();
        var source = appId + appSecret + timeStamp;
        var sign = AlchemyHelper.GenerateAlchemyApiSign(source);
        _logger.LogDebug("appId: {AppId}, timeStamp: {TimeStamp}, signature: {Signature}", appId,
            timeStamp, sign);
        return new Dictionary<string, string>
        {
            ["appId"] = appId,
            ["timestamp"] = timeStamp,
            ["sign"] = sign
        };
    }

}