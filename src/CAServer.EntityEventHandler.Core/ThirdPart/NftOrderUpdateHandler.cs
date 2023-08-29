using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.ThirdPart.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core.ThirdPart;

public class NftOrderUpdateHandler : IDistributedEventHandler<NftOrderEto>
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
    
    public async Task HandleEventAsync(NftOrderEto eventData)
    {
        try
        {
            var nftOrderInfo = _objectMapper.Map<NftOrderEto, NftOrderIndex>(eventData);

            await _nftOrderRepository.AddOrUpdateAsync(nftOrderInfo);

            _logger.LogInformation(
                "nft order index add or update success, Id:{Id}, merchantName:{MerchantName}, merchantOrderId:{MerchantOrderId}",
                eventData.Id, eventData.MerchantName, eventData.MerchantOrderId);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "An error occurred while processing the event, Id:{Id}, merchantName:{MerchantName}, merchantOrderId:{MerchantOrderId}",
                eventData.Id, eventData.MerchantName, eventData.MerchantOrderId);
        }
    }
}