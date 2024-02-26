using Microsoft.Extensions.Logging;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace CAServer.Hub;

public interface IRouteTableProvider
{
    Task SetRouteTableInfoAsync(string cacheKey, string connectionIp, string clientId, string connectionId);
    Task RemoveRouteTableInfoAsync(string cacheKey);
    Task<RouteTableInfo> GetRouteTableInfoAsync(string cacheKey);
}

public class RouteTableProvider : IRouteTableProvider, ISingletonDependency
{
    private readonly IDistributedCache<RouteTableInfo> _distributedCache;
    private readonly ILogger<RouteTableProvider> _logger;

    public RouteTableProvider(IDistributedCache<RouteTableInfo> distributedCache, ILogger<RouteTableProvider> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task SetRouteTableInfoAsync(string cacheKey, string connectionIp, string clientId, string connectionId)
    {
        await _distributedCache.SetAsync(cacheKey, new RouteTableInfo()
        {
            ConnectionIp = connectionIp,
            ClientId = clientId,
            ConnectionId = connectionId
        });

        _logger.LogInformation(
            "set route table info, cacheKey:{cacheKey}, connectionIp:{connectionIp}, clientId:{clientId}, connectionId:{connectionId}",
            cacheKey, connectionIp, clientId, connectionId);
    }

    public async Task RemoveRouteTableInfoAsync(string cacheKey)
    {
        await _distributedCache.RemoveAsync(cacheKey);
        _logger.LogInformation(
            "remove route table info, cacheKey:{cacheKey}", cacheKey);
    }

    public async Task<RouteTableInfo> GetRouteTableInfoAsync(string cacheKey)
    {
        return await _distributedCache.GetAsync(cacheKey);
    }
}