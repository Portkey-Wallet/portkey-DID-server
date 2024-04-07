using System;
using System.Threading.Tasks;
using CAServer.Hubs;
using CAServer.Tab.Etos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.HubsEventHandler;

public class TabHandler : IDistributedEventHandler<TabCompleteEto>, ITransientDependency
{
    private readonly IHubWithCacheService _hubService;
    private readonly ILogger<TabHandler> _logger;
    private readonly IHubContext<CommunicationHub> _hubContext;

    public TabHandler(ILogger<TabHandler> logger, IHubContext<CommunicationHub> hubContext,
        IHubWithCacheService hubService)
    {
        _logger = logger;
        _hubContext = hubContext;
        _hubService = hubService;
    }

    public async Task HandleEventAsync(TabCompleteEto eventData)
    {
        try
        {
            var connectId = await _hubService.GetConnectionIdAsync(eventData.ClientId);
            if (connectId.IsNullOrEmpty())
            {
                _logger.LogWarning("connectId not exist, clientId:{clientId}", eventData.ClientId);
                return;
            }

            var data = new HubResponse<string> { Body = eventData.Data, RequestId = eventData.ClientId };
            await _hubContext.Clients.Client(connectId).SendAsync("caAccountRecover", data);
            _logger.LogInformation("communication success, clientId:{clientId}, methodName:{methodName}",
                eventData.ClientId, eventData.MethodName);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "communication success, clientId:{clientId}, methodName:{methodName}",
                eventData.ClientId, eventData.MethodName);
        }
    }
}