using System;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Tab.Dtos;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.Caching;

namespace CAServer.Hubs;

[HubRoute("communication")]
public class CommunicationHub : AbpHub
{
    private readonly ILogger<CommunicationHub> _logger;
    private readonly IHubWithCacheService _hubService;
    private readonly IDistributedCache<TabCompleteInfo> _distributedCache;
    private const string _cachePrefix = "CaServer:TabCommunication";

    public CommunicationHub(ILogger<CommunicationHub> logger, IHubWithCacheService hubService,
        IDistributedCache<TabCompleteInfo> distributedCache)
    {
        _logger = logger;
        _hubService = hubService;
        _distributedCache = distributedCache;
    }

    public async Task Connect(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            return;
        }

        await _hubService.RegisterClientAsync(clientId, Context.ConnectionId);
        _logger.LogInformation("clientId={clientId}, connectionId={connectionId} connect", clientId,
            Context.ConnectionId);
    }

    public async Task<TabCompleteInfo> GetTabDataAsync(string clientId, string methodName)
    {
        return await _distributedCache.GetAsync(GetKey($"{clientId}:{methodName}"));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _hubService.UnRegisterClientAsync(Context.ConnectionId);
        _logger.LogInformation("connectionId={connectionId} disconnected!!!", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
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