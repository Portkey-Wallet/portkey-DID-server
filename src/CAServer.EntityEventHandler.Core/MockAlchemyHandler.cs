using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using Microsoft.Extensions.Logging;
using MockServer.Dtos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class MockAlchemyHandler : IDistributedEventHandler<AlchemyOrderDto>, ITransientDependency
{
    private readonly INESTRepository<AlchemyOrderIndex, Guid> _orderRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<ThirdPartHandler> _logger;

    public MockAlchemyHandler(INESTRepository<AlchemyOrderIndex, Guid> orderRepository,
        ILogger<ThirdPartHandler> logger, IObjectMapper objectMapper)
    {
        _orderRepository = orderRepository;
        _logger = logger;
        _objectMapper = objectMapper;
    }


    public async Task HandleEventAsync(AlchemyOrderDto eventData)
    {
        try
        {
            var alchemyOrder = _objectMapper.Map<AlchemyOrderDto, AlchemyOrderIndex>(eventData);
            
            await _orderRepository.AddOrUpdateAsync(alchemyOrder);

            _logger.LogInformation("Order{OrderNo} add or update success OrderNo.", eventData.OrderNo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the event,orderId: {OrderNo}", eventData.OrderNo);
        }
    }
}