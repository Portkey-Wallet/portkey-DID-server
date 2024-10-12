using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Monitor.Interceptor;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core.ThirdPart;

public class OrderSettlementUpdateHandler : IDistributedEventHandler<OrderSettlementEto>, ITransientDependency
{
    private readonly INESTRepository<OrderSettlementIndex, Guid> _orderSettlementRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<OrderSettlementUpdateHandler> _logger;

    public OrderSettlementUpdateHandler(INESTRepository<OrderSettlementIndex, Guid> orderSettlementRepository,
        IObjectMapper objectMapper, ILogger<OrderSettlementUpdateHandler> logger)
    {
        _orderSettlementRepository = orderSettlementRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }


    [ExceptionHandler(typeof(Exception),
        Message = "OrderSettlementEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(OrderSettlementEto eventData)
    {

        OrderSettlementGrainDto orderSettlementGrain = eventData?.Data;;
        AssertHelper.NotNull(orderSettlementGrain, "Empty message");
            
        var orderSettlementInfo = _objectMapper.Map<OrderSettlementGrainDto, OrderSettlementIndex>(orderSettlementGrain);

        await _orderSettlementRepository.AddOrUpdateAsync(orderSettlementInfo);

        _logger.LogInformation("Order settlement index add or update success, Id:{Id}", orderSettlementGrain?.Id);
        
    }
}