using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Common.Dtos;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Provider;

public class ExchangeApi
{
    public static ApiInfo GetFreeLoginToken { get; } = new(HttpMethod.Get, "/settlement/currencyrate/conversion-rate");
}

public class ExchangeProvider : ISingletonDependency
{
    private readonly ILogger<ExchangeProvider> _logger;
    private readonly IHttpProvider _httpProvider;
    private readonly IOptionsMonitor<ExchangeApiOptions> _exchangeApiOptions;

    public ExchangeProvider(IOptionsMonitor<ExchangeApiOptions> exchangeApiOptions, IHttpProvider httpProvider,
        ILogger<ExchangeProvider> logger)
    {
        _exchangeApiOptions = exchangeApiOptions;
        _httpProvider = httpProvider;
        _logger = logger;
    }

    public async Task<ExchangeDto> GetMastercardExchange(string fromCurrency, string toCurrency)
    {
        var result = await _httpProvider.Invoke<MastercardExchange>(
            _exchangeApiOptions.CurrentValue.Mastercard.BaseUrl,
            AlchemyApi.QueryNftTrade,
            param: new Dictionary<string, string>
            {
                ["fxDate"] = "0000-00-00",
                ["transCurr"] = fromCurrency,
                ["crdhldBillCurr"] = toCurrency,
                ["transferAmg"] = "1",
                ["bankFee"] = "0"
            }
        );
        AssertHelper.IsTrue(result.Success, "Query mastercard exchange error");
        
        return new ExchangeDto
        {
            FromCurrency = fromCurrency,
            ToCurrency = toCurrency,
            Exchange = result.Data.ConversionRate
        };
    }
}

public class MastercardExchange
{
    public string Type { get; set; }
    public MastercardExchangeData Data { get; set; }

    public bool Success => Type != "error";

}

public class MastercardExchangeData
{
    public string ErrorCode { get; set; }
    public string ErrorMessage { get; set; }
    public string ConversionRate { get; set; }
    public string CrdhldBillAmt { get; set; }
    public string FxDate { get; set; }
    public string TransCurr { get; set; }
    public string CrdhldBillCurr { get; set; }
    public string TransAmt { get; set; }
}