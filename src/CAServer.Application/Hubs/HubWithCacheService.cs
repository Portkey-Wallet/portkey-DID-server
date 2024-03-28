using System;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace CAServer.Hubs;

[RemoteService(false)]
[DisableAuditing]
public class HubWithCacheService : IHubWithCacheService, ISingletonDependency
{
    private readonly IDistributedCache<string> _distributedCache;
    private readonly HubConfigOptions _options;
    private const string _cachePrefix = "CaServer:Hub";

    public HubWithCacheService(IDistributedCache<string> distributedCache, IOptionsSnapshot<HubConfigOptions> options)
    {
        _distributedCache = distributedCache;
        _options = options.Value;
    }

    public async Task RegisterClientAsync(string clientId, string connectionId)
    {
        var key = GetKey(connectionId);
        await _distributedCache.SetAsync(GetKey(clientId), connectionId, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(_options.ExpireDays)
        });

        await _distributedCache.SetAsync(key, clientId, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(_options.ExpireDays)
        });
    }

    public async Task UnRegisterClientAsync(string connectionId)
    {
        var clientId = await _distributedCache.GetAsync(GetKey(connectionId));
        if (clientId.IsNullOrEmpty()) return;

        await _distributedCache.RemoveAsync(GetKey(connectionId));
        await _distributedCache.RemoveAsync(GetKey(clientId));
    }

    public async Task<string> GetConnectionIdAsync(string clientId)
    {
        return await _distributedCache.GetAsync(GetKey(clientId));
    }

    private string GetKey(string key)
    {
        if (key.Contains(CommonConstant.Hyphen) || key.Contains(CommonConstant.Underline))
        {
            key = key.Replace(CommonConstant.Hyphen, CommonConstant.Colon)
                .Replace(CommonConstant.Underline, CommonConstant.Colon);
        }

        return $"{_cachePrefix}:{key}";
    }
}