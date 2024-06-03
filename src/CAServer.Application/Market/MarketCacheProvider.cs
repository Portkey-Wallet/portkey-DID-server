using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.Market;

public interface IMarketCacheProvider
{
    public int GetExpirationMinutes();
}

public class MarketCacheProvider : IMarketCacheProvider, ISingletonDependency
{
    private readonly IOptionsMonitor<MarketCacheOptions> _optionsMonitor;

    public MarketCacheProvider(IOptionsMonitor<MarketCacheOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }
    
    public int GetExpirationMinutes()
    {
        return _optionsMonitor.CurrentValue.ExpireMinutes;
    }
}