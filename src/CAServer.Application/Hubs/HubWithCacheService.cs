using System;
using System.Threading.Tasks;
using CAServer.Cache;
using CAServer.Commons;
using CAServer.Options;
using CAServer.Tab.Dtos;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.Hubs;

public class HubWithCacheService : IHubWithCacheService, ISingletonDependency
{
    private readonly ICacheProvider _cacheProvider;
    private readonly HubConfigOptions _options;

    public HubWithCacheService(IOptionsSnapshot<HubConfigOptions> options, ICacheProvider cacheProvider)
    {
        _cacheProvider = cacheProvider;
        _options = options.Value;
    }

    public async Task RegisterClientAsync(string clientId, string connectionId)
    {
        await _cacheProvider.Set(HubCacheHelper.GetHubCacheKey(clientId), connectionId,
            TimeSpan.FromDays(_options.ExpireDays));
        await _cacheProvider.Set(HubCacheHelper.GetHubCacheKey(connectionId), clientId,
            TimeSpan.FromDays(_options.ExpireDays));
    }

    public async Task UnRegisterClientAsync(string connectionId)
    {
        string clientId = await _cacheProvider.Get(HubCacheHelper.GetHubCacheKey(connectionId));
        if (clientId.IsNullOrEmpty()) return;

        await _cacheProvider.Delete(HubCacheHelper.GetHubCacheKey(clientId));
        await _cacheProvider.Delete(HubCacheHelper.GetHubCacheKey(connectionId));
    }

    public async Task<string> GetConnectionIdAsync(string clientId)
    {
        return await _cacheProvider.Get(HubCacheHelper.GetHubCacheKey(clientId));
    }

    public async Task<TabCompleteInfo> GetTabCompleteInfoAsync(string key)
    {
        return await _cacheProvider.Get<TabCompleteInfo>(HubCacheHelper.GetHubCacheKey(key));
    }
}