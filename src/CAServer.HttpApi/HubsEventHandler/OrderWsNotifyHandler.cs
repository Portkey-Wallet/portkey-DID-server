using System.Threading.Tasks;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos.Order;
using CAServer.ThirdPart.Etos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.HubsEventHandler;

public class OrderWsNotifyHandler : IDistributedEventHandler<OrderEto>, ITransientDependency
{
    
    private readonly IOrderWsNotifyProvider _orderWsNotifyProvider;
    private readonly IObjectMapper _objectMapper;

    public OrderWsNotifyHandler(IObjectMapper objectMapper, IOrderWsNotifyProvider orderWsNotifyProvider)
    {
        _objectMapper = objectMapper;
        _orderWsNotifyProvider = orderWsNotifyProvider;
    }
    
    public async Task HandleEventAsync(OrderEto eventData)
    {
        var notifyOrderDto = _objectMapper.Map<OrderEto, NotifyOrderDto>(eventData);
        await _orderWsNotifyProvider.NotifyOrderDataAsync(notifyOrderDto);
    }
}