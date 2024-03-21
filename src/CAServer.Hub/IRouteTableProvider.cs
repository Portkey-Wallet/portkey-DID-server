using CAServer.Commons;
using Microsoft.Extensions.Logging;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace CAServer.Hub;

public interface IRouteTableProvider
{
    Task SetRouteTableInfoAsync(string clientId, string connectionId);
    Task RemoveRouteTableInfoAsync(string clientId);
    Task<RouteTableInfo> GetRouteTableInfoAsync(string clientId);
}

public class RouteTableProvider : IRouteTableProvider, ISingletonDependency
{
    private readonly IDistributedCache<RouteTableInfo> _distributedCache;
    private readonly ILogger<RouteTableProvider> _logger;
    private readonly HubCacheOptions _hubCacheOptions;

    public RouteTableProvider(IDistributedCache<RouteTableInfo> distributedCache, ILogger<RouteTableProvider> logger,
        HubCacheOptions hubCacheOptions)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _hubCacheOptions = hubCacheOptions;
    }

    public async Task SetRouteTableInfoAsync(string clientId, string connectionId)
    {
        var connectionIp = IpHelper.LocalIp;
        if (!_hubCacheOptions.RouteTableConfig.LocalIp.IsNullOrEmpty())
        {
            connectionIp = _hubCacheOptions.RouteTableConfig.LocalIp;
        }

        var cacheKey = IpHelper.GetRouteTableKey(clientId, _hubCacheOptions.RouteTableConfig.Port, connectionIp);
        await _distributedCache.SetAsync(cacheKey, new RouteTableInfo()
        {
            ConnectionIp = connectionIp,
            Port = _hubCacheOptions.RouteTableConfig.Port,
            ClientId = clientId,
            ConnectionId = connectionId
        });

        _logger.LogInformation(
            "set route table info, cacheKey:{cacheKey}, connectionIp:{connectionIp}, clientId:{clientId}, connectionId:{connectionId}",
            cacheKey, connectionIp, clientId, connectionId);
    }

    public async Task RemoveRouteTableInfoAsync(string clientId)
    {
        var cacheKey = IpHelper.GetRouteTableKey(clientId, _hubCacheOptions.RouteTableConfig.Port,
            _hubCacheOptions.RouteTableConfig.LocalIp);
        await _distributedCache.RemoveAsync(cacheKey);
        _logger.LogInformation(
            "remove route table info, cacheKey:{cacheKey}", cacheKey);
    }

    public async Task<RouteTableInfo> GetRouteTableInfoAsync(string clientId)
    {
        var cacheKey = IpHelper.GetRouteTableKey(clientId, _hubCacheOptions.RouteTableConfig.Port,
            _hubCacheOptions.RouteTableConfig.LocalIp);
        return await _distributedCache.GetAsync(cacheKey);
    }
}