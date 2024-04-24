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
            await SendAsync(eventData.ClientId, eventData.MethodName, eventData.Data);
            if (!eventData.TargetClientId.IsNullOrEmpty())
            {
                _logger.LogInformation(
                    "begin send to targetClientId, clientId:{clientId}, methodName:{methodName}, targetClientId:{targetClientId}",
                    eventData.ClientId, eventData.MethodName, eventData.TargetClientId);
                
                await SendAsync(eventData.TargetClientId, eventData.MethodName, eventData.Data);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "communication success, clientId:{clientId}, methodName:{methodName}",
                eventData.ClientId, eventData.MethodName);
        }
    }

    private async Task SendAsync(string clientId, string methodName, string data)
    {
        var connectId = await _hubService.GetConnectionIdAsync(clientId);
        if (connectId.IsNullOrEmpty())
        {
            _logger.LogWarning("connectId not exist, clientId:{clientId}", clientId);
        }

        await SendAsync(connectId, clientId, methodName, data);
    }

    private async Task SendAsync(string connectId, string clientId, string methodName, string data)
    {
        try
        {
            await _hubContext.Clients.Client(connectId).SendAsync(methodName,
                new HubResponse<string> { Body = data, RequestId = clientId });
            _logger.LogInformation("send success, clientId:{clientId}, methodName:{methodName}", clientId,
                methodName);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "send error, clientId:{clientId}, methodName:{methodName}", clientId, methodName);
        }
    }
}