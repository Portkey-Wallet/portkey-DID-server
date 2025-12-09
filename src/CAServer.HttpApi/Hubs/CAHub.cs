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

        await _hubService.RegisterClient(clientId, Context.ConnectionId);
        _logger.LogInformation("clientId={ClientId} connect", clientId);
        await _hubService.SendAllUnreadRes(clientId);
    }

    public async Task Ack(string clientId, string requestId)
    {
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(requestId))
        {
            return;
        }

        await _hubService.Ack(clientId, requestId);
    }

    public async Task RequestAchTxAddress(OrderListenerRequestDto request)
    {
        await _hubService.RequestAchTxAddressAsync(request.TargetClientId, request.OrderId);
    }

    public async Task RequestOrderTransferred(OrderListenerRequestDto request)
    {
        await _hubService.RequestOrderTransferredAsync(request.TargetClientId, request.OrderId);
    }
    
    public async Task RequestNFTOrderStatus(OrderListenerRequestDto request)
    {
        await _hubService.RequestNFTOrderStatusAsync(request.TargetClientId, request.OrderId);
    }

    public async Task RequestRampOrderStatus(OrderListenerRequestDto request)
    {
        await _hubService.RequestRampOrderStatus(request.TargetClientId, request.OrderId);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var clientId = _hubService.UnRegisterClient(Context.ConnectionId);
        _logger.LogInformation("clientId={ClientId} disconnected!!!", clientId);
        return base.OnDisconnectedAsync(exception);
    }
}