using System;
using System.Threading.Tasks;
using CAServer.Message.Dtos;
using CAServer.Message.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.Hubs;

public class CAHub : AbpHub
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IHubService _hubService;
    private readonly ILogger<CAHub> _logger;

    public CAHub(ILogger<CAHub> logger, IHubService hubService, IDistributedEventBus distributedEventBus)
    {
        _logger = logger;
        _hubService = hubService;
        _distributedEventBus = distributedEventBus;
    }


    public async Task Connect(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            return;
        }

        _hubService.RegisterClient(clientId, Context.ConnectionId);
        _logger.LogInformation($"clientId={clientId} connect");
        _hubService.SendAllUnreadRes(clientId);
    }

    public async Task Ack(string clientId, string requestId)
    {
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(requestId))
        {
            return;
        }

        _hubService.Ack(clientId, requestId);
    }

    public async Task BindAlchemyOrder(AlchemyTargetAddressDto request)
    {
        await _distributedEventBus.PublishAsync(new AlchemyTargetAddressDto()
        {
            TargetClientId = request.TargetClientId,
            OrderId = request.OrderId
        });
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var clientId = _hubService.UnRegisterClient(Context.ConnectionId);
        _logger.LogInformation($"clientId={clientId} disconnected!!!");
        return base.OnDisconnectedAsync(exception);
    }
}