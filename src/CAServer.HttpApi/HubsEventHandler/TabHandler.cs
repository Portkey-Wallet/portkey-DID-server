using System.Threading.Tasks;
using CAServer.Hub;
using CAServer.Hubs;
using CAServer.Tab.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.HubsEventHandler;

public class TabHandler : IDistributedEventHandler<TabCompleteEto>, ITransientDependency
{
    private readonly IHubProvider _caHubProvider;
    private readonly ILogger<TabHandler> _logger;
    private readonly IDistributedCache<RouteTableInfo> _distributedCache;
    private readonly HubCacheOptions _hubCacheOptions;

    public TabHandler(IHubProvider caHubProvider, ILogger<TabHandler> logger,
        IDistributedCache<RouteTableInfo> distributedCache, HubCacheOptions hubCacheOptions)
    {
        _caHubProvider = caHubProvider;
        _logger = logger;
        _distributedCache = distributedCache;
        _hubCacheOptions = hubCacheOptions;
        // _routeTableProvider = routeTableProvider;
    }

    public async Task HandleEventAsync(TabCompleteEto eventData)
    {
      //  await _routeTableProvider.GetRouteTableInfoAsync("");
        await _caHubProvider.ResponseAsync(
            new Hubs.HubResponse<string> { Body = eventData.Data, RequestId = eventData.ClientId },
            eventData.ClientId, eventData.MethodName);

        _logger.LogInformation("tab communication success, clientId:{clientId}, methodName:{methodName}",
            eventData.ClientId, eventData.MethodName);
    }
}