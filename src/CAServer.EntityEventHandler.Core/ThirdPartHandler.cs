using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.LinqToElasticSearch.Provider;
using CAServer.Entities.Es;
using CAServer.ThirdPart.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class ThirdPartHandler : IDistributedEventHandler<OrderEto>, ITransientDependency
{
    private readonly ILinqRepository<OrderIndex, Guid> _orderRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<ThirdPartHandler> _logger;

    public ThirdPartHandler(
        ILinqRepository<OrderIndex, Guid> orderRepository,
        IObjectMapper objectMapper,
        ILogger<ThirdPartHandler> logger)
    {
        _orderRepository = orderRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(OrderEto eventData)
    {
        try
        {
            var userOrder = _objectMapper.Map<OrderEto, OrderIndex>(eventData);

            await _orderRepository.AddOrUpdateAsync(userOrder);

            _logger.LogInformation($"Order{eventData.Id} add or update success orderId.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while processing the event,orderId: {eventData.Id}");
        }
    }
}