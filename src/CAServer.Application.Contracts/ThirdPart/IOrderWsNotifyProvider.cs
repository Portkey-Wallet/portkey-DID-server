using System;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos.Order;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart;

public interface IOrderWsNotifyProvider : ISingletonDependency
{
    Task RegisterOrderListenerAsync(string clientId, string orderId, Func<NotifyOrderDto, Task> callback);
    Task UnRegisterOrderListenerAsync(string orderId);
    Task NotifyOrderDataAsync(NotifyOrderDto orderDto);
}