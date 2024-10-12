using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Monitor.Interceptor;
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

    [ExceptionHandler(typeof(Exception),
        Message = "ThirdPartHandler OrderEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(OrderEto eventData)
    {
        var userOrder = _objectMapper.Map<OrderEto, RampOrderIndex>(eventData);

        await _orderRepository.AddOrUpdateAsync(userOrder);

        _logger.LogInformation($"Order{eventData.Id} add or update success orderId.");
    }

    [ExceptionHandler(typeof(Exception),
        Message = "ThirdPartHandler OrderStatusInfoEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(OrderStatusInfoEto eventData)
    {
        var orderStatusInfo = _objectMapper.Map<OrderStatusInfoEto, OrderStatusInfoIndex>(eventData);

        await _orderStatusInfoRepository.AddOrUpdateAsync(orderStatusInfo);

        _logger.LogInformation("Order status add or update success, statusId:{id}, orderId:{orderId}", eventData.Id,
            eventData.OrderId);
    }

}