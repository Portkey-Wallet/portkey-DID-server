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

    public ExchangeProvider(IOptionsMonitor<ExchangeApiOptions> exchangeApiOptions, IHttpProvider httpProvider, ILogger<ExchangeProvider> logger)
    {
        _exchangeApiOptions = exchangeApiOptions;
        _httpProvider = httpProvider;
        _logger = logger;
    }

    public Task<ExchangeDto> GetMastercardExchange(string fromCurrency, string toCurrency)
    {

        // TODO nzc
        return null;
    }
    
    
    
}