using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Http;
using CAServer.Http.Dtos;
using CAServer.Options;
using CAServer.Tokens.Dtos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CAServer.Tokens.Provider;

public static class OkxApi
{
    public static ApiInfo KLine = new(HttpMethod.Get, "/api/v5/market/index-candles");
    public static ApiInfo KLineHistory = new(HttpMethod.Get, "/api/v5/market/history-index-candles");
}

public class OkxProvider : IExchangeProvider
{
    private readonly IOptionsMonitor<ExchangeOptions> _exchangeOptions;
    private readonly IHttpProvider _httpProvider;

    public OkxProvider(IOptionsMonitor<ExchangeOptions> exchangeOptions, IHttpProvider httpProvider)
    {
        _exchangeOptions = exchangeOptions;
        _httpProvider = httpProvider;
    }

    public string BaseUrl()
    {
        return _exchangeOptions.CurrentValue.Okx.BaseUrl;
    }


    public ExchangeProviderName Name()
    {
        return ExchangeProviderName.Okx;
    }

    public async Task<TokenExchange> LatestAsync(string fromSymbol, string toSymbol)
    {
        // The first k-line after one minute of inquiry returns the latest price.
        var req = new OkxKLineReq()
        {
            InstId = string.Join("-", fromSymbol.ToUpper(), toSymbol.ToUpper()),
            After = DateTime.UtcNow.AddMinutes(1).ToUtcMilliSeconds().ToString(),
            Bar = Bar.Minute5,
            Limit = "1"
        };

        var res = await _httpProvider.InvokeAsync<OkxResponse<List<List<string>>>>(BaseUrl(), OkxApi.KLine,
            param: JsonConvert.DeserializeObject<Dictionary<string, string>>(
                JsonConvert.SerializeObject(req, HttpProvider.DefaultJsonSettings))
        );
        AssertHelper.IsTrue(res.Success, "Query okx kline failed, msg={Msg}", res.Message);
        AssertHelper.NotEmpty(res.Data, "Query okx kline empty");

        var kline = OkxKLine.FromArray(res.Data[0]);
        AssertHelper.NotNull(kline, "Okx key line format error, item = {Item}", string.Join(",", res.Data[0]));

        // If the k-line ends, take the average of the highest and lowest prices,
        // if not, the closing price is the latest price.
        return new TokenExchange
        {
            FromSymbol = fromSymbol,
            ToSymbol = toSymbol,
            Exchange = kline.Finished ? kline.AvgAmount() : kline.EndAmount.SafeToDecimal(),
            Timestamp = DateTime.UtcNow.WithSeconds(0).WithMicroSeconds(0).WithMilliSeconds(0).ToUtcMilliSeconds()
        };
    }

    public async Task<TokenExchange> HistoryAsync(string fromSymbol, string toSymbol, long timestamp)
    {
        // The first k-line after one minute of inquiry returns the latest price.
        var req = new OkxKLineReq()
        {
            InstId = string.Join("-", fromSymbol.ToUpper(), toSymbol.ToUpper()),
            After = TimeHelper.GetDateTimeFromTimeStamp(timestamp).WithSeconds(0).WithMicroSeconds(0)
                .WithMilliSeconds(1).ToUtcMilliSeconds().ToString(),
            Bar = Bar.Minute1,
            Limit = "1"
        };

        var res = await _httpProvider.InvokeAsync<OkxResponse<List<List<string>>>>(BaseUrl(), OkxApi.KLineHistory,
            param: JsonConvert.DeserializeObject<Dictionary<string, string>>(
                JsonConvert.SerializeObject(req, HttpProvider.DefaultJsonSettings))
        );
        AssertHelper.IsTrue(res.Success, "Query okx kline failed, msg={Msg}", res.Message);
        AssertHelper.NotEmpty(res.Data, "Query okx kline empty");

        var kline = OkxKLine.FromArray(res.Data[0]);
        AssertHelper.NotNull(kline, "Okx key line format error, item = {Item}", string.Join(",", res.Data[0]));
        
        // If the k-line ends, take the average of the highest and lowest prices,
        // if not, the closing price is the latest price.
        return new TokenExchange
        {
            FromSymbol = fromSymbol,
            ToSymbol = toSymbol,
            Exchange = kline.Finished ? kline.AvgAmount() : kline.EndAmount.SafeToDecimal(),
            Timestamp = DateTime.UtcNow.WithSeconds(0).WithMicroSeconds(0).WithMilliSeconds(0).ToUtcMilliSeconds()
        };
    }
}

public class OkxResponse<TData>
{
    public string Code { get; set; } = "0";
    public string Message { get; set; }
    public bool Success => Code == "0";
    public TData Data { get; set; }
}

public class OkxKLineReq
{
    public string InstId { get; set; }
    public string After { get; set; }
    public string Before { get; set; }
    public string Bar { get; set; }
    public string Limit { get; set; }
}

public class OkxKLine
{
    public string StartTime { get; set; }
    public string EndTime { get; set; }
    public string StartAmount { get; set; }
    public string EndAmount { get; set; }
    public string MinAmount { get; set; }
    public string MaxAmount { get; set; }
    public bool Finished { get; set; }

    public void WithEndTime(string bar)
    {
        var startTime = TimeHelper.GetDateTimeFromTimeStamp(StartTime.SafeToLong());
        AssertHelper.NotEmpty(EndTime, "Invalid okx kline startTime");

        EndTime = bar == Bar.Minute1 ? startTime.AddMinutes(1).ToUtcMilliSeconds().ToString()
            : bar == Bar.Minute3 ? startTime.AddMinutes(3).ToUtcMilliSeconds().ToString()
            : bar == Bar.Minute5 ? startTime.AddMinutes(5).ToUtcMilliSeconds().ToString()
            : bar == Bar.Minute15 ? startTime.AddMinutes(15).ToUtcMilliSeconds().ToString()
            : bar == Bar.Minute30 ? startTime.AddMinutes(30).ToUtcMilliSeconds().ToString()
            : bar == Bar.Hour1 ? startTime.AddHours(1).ToUtcMilliSeconds().ToString()
            : bar == Bar.Hour2 ? startTime.AddHours(2).ToUtcMilliSeconds().ToString()
            : bar == Bar.Hour4 ? startTime.AddHours(4).ToUtcMilliSeconds().ToString()
            : null;
        AssertHelper.NotEmpty(EndTime, "Invalid bar {Bar}", bar);
    }

    public decimal AvgAmount()
    {
        var avg = (MaxAmount.SafeToDecimal() + MinAmount.SafeToDecimal()) / 2;
        return avg;
    }

    public static OkxKLine FromArray(List<string> arrayItem)
    {
        AssertHelper.NotEmpty(arrayItem, "Okx kline array empty");
        AssertHelper.IsTrue(arrayItem.Count >= 6, "Okx kline array empty");
        return new OkxKLine
        {
            StartTime = arrayItem[0],
            StartAmount = arrayItem[1],
            MaxAmount = arrayItem[2],
            MinAmount = arrayItem[3],
            EndAmount = arrayItem[4],
            Finished = arrayItem[5] == "1",
        };
    }
}

public static class Bar
{
    public static string Minute1 = "1m";
    public static string Minute3 = "3m";
    public static string Minute5 = "5m";
    public static string Minute15 = "15m";
    public static string Minute30 = "30m";
    public static string Hour1 = "1H";
    public static string Hour2 = "2H";
    public static string Hour4 = "4H";
}