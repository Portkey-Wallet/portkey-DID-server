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

    public async Task<TabCompleteInfo> GetTabDataAsync(TabDataRequestDto input)
    {
        _logger.LogInformation("GetTabData clientId:{clientId},methodName:{methodName}", input.ClientId,
            input.MethodName);
        return await _distributedCache.GetAsync(HubCacheHelper.GetTabKey($"{input.ClientId}:{input.MethodName}"));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _hubService.UnRegisterClientAsync(Context.ConnectionId);
        _logger.LogInformation("connectionId={connectionId} disconnected!!!", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}