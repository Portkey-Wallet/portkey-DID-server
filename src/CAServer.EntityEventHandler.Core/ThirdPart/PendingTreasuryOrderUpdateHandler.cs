using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core.ThirdPart;

public class PendingTreasuryOrderUpdateHandler : IDistributedEventHandler<PendingTreasuryOrderEto>, ITransientDependency
{
    private readonly INESTRepository<PendingTreasuryOrderIndex, Guid> _pendingTreasuryOrderRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<PendingTreasuryOrderUpdateHandler> _logger;

    public PendingTreasuryOrderUpdateHandler(INESTRepository<PendingTreasuryOrderIndex, Guid> pendingTreasuryOrderRepository,
        IObjectMapper objectMapper, ILogger<PendingTreasuryOrderUpdateHandler> logger)
    {
        _pendingTreasuryOrderRepository = pendingTreasuryOrderRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(PendingTreasuryOrderEto eventData)
    {
        PendingTreasuryOrderDto orderGrainDto = null;
        try
        {
            orderGrainDto = eventData?.Data;
            AssertHelper.NotNull(orderGrainDto, "Empty message");

            
            var nftOrderInfo = _objectMapper.Map<PendingTreasuryOrderDto, PendingTreasuryOrderIndex>(orderGrainDto);

            await _pendingTreasuryOrderRepository.AddOrUpdateAsync(nftOrderInfo);

            _logger.LogInformation(
                "Pending Treasury order index add or update success, Id:{Id}, ThirdPartName:{ThirdPartName}, ThirdPartOrderId:{ThirdPartOrderId}",
                orderGrainDto?.Id, orderGrainDto?.ThirdPartName, orderGrainDto?.ThirdPartOrderId);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "An error occurred while processing the event, Id:{Id}, ThirdPartName:{ThirdPartName}, ThirdPartOrderId:{ThirdPartOrderId}",
                orderGrainDto?.Id, orderGrainDto?.ThirdPartName, orderGrainDto?.ThirdPartOrderId);
        }
        
        
    }
}