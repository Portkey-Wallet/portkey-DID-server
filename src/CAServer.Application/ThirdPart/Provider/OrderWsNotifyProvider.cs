using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.ThirdPart.Dtos.Order;
using CAServer.Tokens.Provider;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Provider;


public class OrderWsNotifyProvider : IOrderWsNotifyProvider
{
    
    // clientId => orderId
    private readonly Dictionary<string, string> _clientOrderListener = new();
    
    // orderId => callback func
    private readonly Dictionary<string, Func<NotifyOrderDto, Task>> _orderNotifyListeners = new();
    
    private readonly ITokenProvider _tokenProvider;

    public OrderWsNotifyProvider(ITokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    public Task RegisterOrderListenerAsync(string clientId, string orderId, Func<NotifyOrderDto, Task> callback)
    {
        UnRegisterOrderListenerAsync(clientId);
        _clientOrderListener[clientId] = orderId;
        _orderNotifyListeners[orderId] = callback;
        return Task.CompletedTask;
    }
    
    public Task UnRegisterOrderListenerAsync(string clientId)
    {
        if (clientId.IsNullOrEmpty()) return Task.CompletedTask;
        
        if (_clientOrderListener.TryGetValue(clientId, out var orderId))
        {
            _orderNotifyListeners.Remove(orderId);
            _clientOrderListener.Remove(clientId);
        }
        return Task.CompletedTask;
    }

    public async Task NotifyOrderDataAsync(NotifyOrderDto orderDto)
    {
        if (orderDto == null) return;
        
        orderDto.DisplayStatus =
            orderDto.IsNftOrder()
                ? OrderDisplayStatus.ToNftCheckoutRampDisplayStatus(orderDto.Status)
                : orderDto.TransDirect == TransferDirectionType.TokenBuy.ToString()
                    ? OrderDisplayStatus.ToOnRampDisplayStatus(orderDto.Status)
                    : OrderDisplayStatus.ToOffRampDisplayStatus(orderDto.Status);
        if (orderDto.Crypto.NotNullOrEmpty())
        {
            var tokenInfo = await _tokenProvider.GetTokenInfoAsync(CommonConstant.MainChainId, orderDto.Crypto);
            if (tokenInfo != null)
            {
                orderDto.CryptoDecimals = tokenInfo.Decimals.ToString();
            }
        }
        if (_orderNotifyListeners.TryGetValue(orderDto.OrderId.ToString(), out var callback))
        {
            await callback.Invoke(orderDto);
        }
    }
    
}