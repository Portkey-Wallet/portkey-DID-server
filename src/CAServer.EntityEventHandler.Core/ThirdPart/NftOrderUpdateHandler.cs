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
using Volo.Abp.TenantManagement;

namespace CAServer.EntityEventHandler.Core.ThirdPart;

public class NftOrderUpdateHandler : IDistributedEventHandler<NftOrderEto>, ITransientDependency
{
    private readonly INESTRepository<NftOrderIndex, Guid> _nftOrderRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<NftOrderUpdateHandler> _logger;

    public NftOrderUpdateHandler(INESTRepository<NftOrderIndex, Guid> nftOrderRepository, IObjectMapper objectMapper,
        ILogger<NftOrderUpdateHandler> logger)
    {
        _nftOrderRepository = nftOrderRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }
    
    [ExceptionHandler(typeof(Exception),
        Message = "NftOrderEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(NftOrderEto eventData)
    {
        NftOrderGrainDto nftOrderGrainDto = eventData?.Data;
        AssertHelper.NotNull(nftOrderGrainDto, "Empty message");

        var nftOrderInfo = _objectMapper.Map<NftOrderGrainDto, NftOrderIndex>(nftOrderGrainDto);
        await _nftOrderRepository.AddOrUpdateAsync(nftOrderInfo);

        _logger.LogInformation(
            "NftOrderEto nft order index add or update success, Id:{Id}, merchantName:{MerchantName}, merchantOrderId:{MerchantOrderId}",
            nftOrderGrainDto?.Id, nftOrderGrainDto?.MerchantName, nftOrderGrainDto?.MerchantOrderId);
    }
}