using System.Threading.Tasks;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos.Order;
using CAServer.ThirdPart.Etos;
using MassTransit;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace CAServer.HubsEventHandler;

public class OrderWsBroadcastConsumer : IConsumer<OrderEto>, ITransientDependency
{
    
    private readonly IOrderWsNotifyProvider _orderWsNotifyProvider;
    private readonly IObjectMapper _objectMapper;

    public OrderWsBroadcastConsumer(IObjectMapper objectMapper, IOrderWsNotifyProvider orderWsNotifyProvider)
    {
        _objectMapper = objectMapper;
        _orderWsNotifyProvider = orderWsNotifyProvider;
    }
    
    public async Task Consume(ConsumeContext<OrderEto> eventData)
    {
        var notifyOrderDto = _objectMapper.Map<OrderEto, NotifyOrderDto>(eventData.Message);
        await _orderWsNotifyProvider.NotifyOrderDataAsync(notifyOrderDto);
    }
}