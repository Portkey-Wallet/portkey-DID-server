using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.ThirdPart.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core.ThirdPart;

public class ThirdPartHandler : IDistributedEventHandler<OrderEto>, IDistributedEventHandler<OrderStatusInfoEto>,
    ITransientDependency
{
    private readonly INESTRepository<RampOrderIndex, Guid> _orderRepository;
    private readonly INESTRepository<OrderStatusInfoIndex, string> _orderStatusInfoRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<ThirdPartHandler> _logger;

    public ThirdPartHandler(
        INESTRepository<RampOrderIndex, Guid> orderRepository,
        IObjectMapper objectMapper,
        ILogger<ThirdPartHandler> logger,
        INESTRepository<OrderStatusInfoIndex, string> orderStatusInfoRepository)
    {
        _orderRepository = orderRepository;
        _objectMapper = objectMapper;
        _logger = logger;
        _orderStatusInfoRepository = orderStatusInfoRepository;
    }

    public async Task HandleEventAsync(OrderEto eventData)
    {
        try
        {
            var userOrder = _objectMapper.Map<OrderEto, RampOrderIndex>(eventData);

            await _orderRepository.AddOrUpdateAsync(userOrder);

            _logger.LogInformation($"Order{eventData.Id} add or update success orderId.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while processing the event,orderId: {eventData.Id}");
        }
    }

    public async Task HandleEventAsync(OrderStatusInfoEto eventData)
    {
        try
        {
            var orderStatusInfo = _objectMapper.Map<OrderStatusInfoEto, OrderStatusInfoIndex>(eventData);

            await _orderStatusInfoRepository.AddOrUpdateAsync(orderStatusInfo);

            _logger.LogInformation("Order status add or update success, statusId:{id}, orderId:{orderId}", eventData.Id,
                eventData.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the event, statusId:{id}, orderId:{orderId}",
                eventData.Id,
                eventData.OrderId);
        }
    }

}