using System;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Hub;
using CAServer.Message.Dtos;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.SignalR;

namespace CAServer.Hubs;

public class CAHub : AbpHub
{
    private readonly IHubService _hubService;
    private readonly IRouteTableProvider _routeTableProvider;
    private readonly ILogger<CAHub> _logger;

    public CAHub(ILogger<CAHub> logger, IHubService hubService, IRouteTableProvider routeTableProvider)
    {
        _logger = logger;
        _hubService = hubService;
        _routeTableProvider = routeTableProvider;
    }


    public async Task Connect(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            return;
        }

        await _hubService.RegisterClient(clientId, Context.ConnectionId);
        var ip = IpHelper.GetLocalIp();
        await _routeTableProvider.SetRouteTableInfoAsync(IpHelper.GetRouteTableKey(ip, Context.ConnectionId), ip,
            clientId,
            Context.ConnectionId);
        _logger.LogInformation("clientId={clientId} connect", clientId);
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

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var clientId = _hubService.UnRegisterClient(Context.ConnectionId);
        await _routeTableProvider.RemoveRouteTableInfoAsync(IpHelper.GetRouteTableKey(Context.ConnectionId));
        _logger.LogInformation("clientId={clientId} disconnected!!!", clientId);
        await base.OnDisconnectedAsync(exception);
    }
}