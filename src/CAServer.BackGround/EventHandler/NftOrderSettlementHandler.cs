using CAServer.ThirdPart;
using CAServer.ThirdPart.Etos;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.BackGround.EventHandler;

public class NftOrderSettlementHandler : IDistributedEventHandler<OrderEto>, ITransientDependency
{
    private static readonly List<string> NftReleaseResultStatus = new()
    {
        OrderStatusType.Finish.ToString(),
    };

    private readonly ILogger<NftOrderSettlementHandler> _logger;
    private readonly INftCheckoutService _nftCheckoutService;
    
    public NftOrderSettlementHandler(ILogger<NftOrderSettlementHandler> logger,
        INftCheckoutService nftCheckoutService)
    {
        _logger = logger;
        _nftCheckoutService = nftCheckoutService;
    }

    private static bool Match(OrderEto eventData)
    {
        return eventData.TransDirect == TransferDirectionType.NFTBuy.ToString()
               && NftReleaseResultStatus.Contains(eventData.Status);
    }

    public async Task HandleEventAsync(OrderEto eventData)
    {
        // verify event is NFT release result
        if (!Match(eventData)) return;

        try
        {
            await _nftCheckoutService.GetProcessor(eventData.MerchantName)
                .SaveOrderSettlementAsync(eventData.Id);
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(e, "Notify nft release result fail, Id={Id}, Status={Status}",
                eventData.Id, eventData.Status);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Notify nft release result error, Id={Id}, Status={Status}",
                eventData.Id, eventData.Status);
            throw;
        }
    }
}