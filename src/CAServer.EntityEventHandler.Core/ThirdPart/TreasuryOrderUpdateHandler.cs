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

public class TreasuryOrderUpdateHandler : IDistributedEventHandler<TreasuryOrderEto>, ITransientDependency
{
    private readonly INESTRepository<TreasuryOrderIndex, Guid> _nftOrderRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<TreasuryOrderUpdateHandler> _logger;

    public TreasuryOrderUpdateHandler(INESTRepository<TreasuryOrderIndex, Guid> nftOrderRepository,
        IObjectMapper objectMapper, ILogger<TreasuryOrderUpdateHandler> logger)
    {
        _nftOrderRepository = nftOrderRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "ThirdPartHandler OrderStatusInfoEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(TreasuryOrderEto eventData)
    {
        TreasuryOrderDto orderGrainDto = eventData?.Data;
        AssertHelper.NotNull(orderGrainDto, "Receive empty message");
            
        var nftOrderInfo = _objectMapper.Map<TreasuryOrderDto, TreasuryOrderIndex>(orderGrainDto);

        await _nftOrderRepository.AddOrUpdateAsync(nftOrderInfo);

        _logger.LogInformation(
            "Treasury order index add or update success, Id:{Id}, ThirdPartName:{ThirdPartName}, ThirdPartOrderId:{ThirdPartOrderId}",
            orderGrainDto?.Id, orderGrainDto?.ThirdPartName, orderGrainDto?.ThirdPartOrderId);
    }
}