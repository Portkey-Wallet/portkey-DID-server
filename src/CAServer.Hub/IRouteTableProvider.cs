using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace CAServer.Hub;

public interface IRouteTableProvider
{
    Task SetIpTableInfoAsync(string cacheKey, string connectionIp, string clientId, string connectionId);
    Task RemoveIpTableInfoAsync(string cacheKey);
    Task<IpTableInfo> GetIpTableInfoAsync(string cacheKey);
}

public class RouteTableProvider : IRouteTableProvider, ISingletonDependency
{
    private readonly IDistributedCache<IpTableInfo> _distributedCache;

    public RouteTableProvider(IDistributedCache<IpTableInfo> distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task SetIpTableInfoAsync(string cacheKey, string connectionIp, string clientId, string connectionId)
    {
        await _distributedCache.SetAsync(cacheKey, new IpTableInfo()
        {
            ConnectionIp = connectionIp,
            ClientId = clientId,
            ConnectionId = connectionId
        });
    }

    public async Task RemoveIpTableInfoAsync(string cacheKey)
    {
        await _distributedCache.RemoveAsync(cacheKey);
    }

    public async Task<IpTableInfo> GetIpTableInfoAsync(string cacheKey)
    {
        return await _distributedCache.GetAsync(cacheKey);
    }
}