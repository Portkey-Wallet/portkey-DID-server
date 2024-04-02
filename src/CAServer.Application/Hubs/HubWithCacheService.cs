using System;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace CAServer.Hubs;

public class HubWithCacheService : IHubWithCacheService, ISingletonDependency
{
    private readonly IDistributedCache<string> _distributedCache;
    private readonly HubConfigOptions _options;

    public HubWithCacheService(IDistributedCache<string> distributedCache, IOptionsSnapshot<HubConfigOptions> options)
    {
        _distributedCache = distributedCache;
        _options = options.Value;
    }

    public async Task RegisterClientAsync(string clientId, string connectionId)
    {
        await _distributedCache.SetAsync(HubCacheHelper.GetHubCacheKey(clientId), connectionId,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(_options.ExpireDays)
            });

        await _distributedCache.SetAsync(HubCacheHelper.GetHubCacheKey(connectionId), clientId,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(_options.ExpireDays)
            });
    }

    public async Task UnRegisterClientAsync(string connectionId)
    {
        var clientId = await _distributedCache.GetAsync(HubCacheHelper.GetHubCacheKey(connectionId));
        if (clientId.IsNullOrEmpty()) return;

        await _distributedCache.RemoveAsync(HubCacheHelper.GetHubCacheKey(connectionId));
        await _distributedCache.RemoveAsync(HubCacheHelper.GetHubCacheKey(clientId));
    }

    public async Task<string> GetConnectionIdAsync(string clientId)
    {
        return await _distributedCache.GetAsync(HubCacheHelper.GetHubCacheKey(clientId));
    }
}