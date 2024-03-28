using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.SignalR;

namespace CAServer.Hubs;

[HubRoute("communication")]
public class CommunicationHub : AbpHub
{
    private readonly ILogger<CommunicationHub> _logger;
    private readonly IHubWithCacheService _hubService;

    public CommunicationHub(ILogger<CommunicationHub> logger, IHubWithCacheService hubService)
    {
        _logger = logger;
        _hubService = hubService;
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

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
         await _hubService.UnRegisterClientAsync(Context.ConnectionId);
         _logger.LogInformation("connectionId={connectionId} disconnected!!!", Context.ConnectionId);
         await base.OnDisconnectedAsync(exception);
    }
}