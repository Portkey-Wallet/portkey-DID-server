using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos.Order;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Provider;


public class OrderWsNotifyProvider : IOrderWsNotifyProvider
{
    
    // clientId => orderId
    private readonly Dictionary<string, string> _clientOrderListener = new();
    
    // orderId => callback func
    private readonly Dictionary<string, Func<NotifyOrderDto, Task>> _orderNotifyListeners = new();

    public Task RegisterOrderListenerAsync(string clientId, string orderId, Func<NotifyOrderDto, Task> callback)
    {
        _clientOrderListener[clientId] = orderId;
        _orderNotifyListeners[orderId] = callback;
        return Task.CompletedTask;
    }
    
    public Task UnRegisterOrderListenerAsync(string clientId)
    {
        if (_clientOrderListener.TryGetValue(clientId ?? string.Empty, out var orderId))
        {
            _orderNotifyListeners.Remove(orderId);
        }
        return Task.CompletedTask;
    }

    public Task NotifyOrderDataAsync(NotifyOrderDto orderDto)
    {
        if (orderDto == null)
        {
            return Task.CompletedTask;
        }
        if (_orderNotifyListeners.TryGetValue(orderDto.OrderId.ToString(), out var callback))
        {
            callback.Invoke(orderDto);
        }
        return Task.CompletedTask;
    }
    
}