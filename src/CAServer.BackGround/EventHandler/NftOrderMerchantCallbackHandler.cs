using CAServer.Common;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Provider;
using Orleans;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.BackGround.EventHandler;

public class NftOrderMerchantCallbackHandler : IDistributedEventHandler<OrderEto>, ITransientDependency
{
    private static readonly List<string> ResultStatus = new()
    {
        OrderStatusType.Finish.ToString(),
    };

    private readonly ILogger<NftOrderMerchantCallbackHandler> _logger;
    private readonly IOrderStatusProvider _orderStatusProvider;

    public NftOrderMerchantCallbackHandler(ILogger<NftOrderMerchantCallbackHandler> logger,
        IOrderStatusProvider orderStatusProvider)
    {
        _logger = logger;
        _orderStatusProvider = orderStatusProvider;
    }

    private static bool Match(OrderEto eventData)
    {
        return eventData.TransDirect == TransferDirectionType.NFTBuy.ToString()
               && ResultStatus.Contains(eventData.Status);
    }

    public async Task HandleEventAsync(OrderEto eventData)
    {
        // verify event is NFT pay result
        if (!Match(eventData)) return;

        var orderId = eventData.Id;
        var status = eventData.Status;
        try
        {
            // callback merchant and update result
            await _orderStatusProvider.CallBackNftOrderPayResultAsync(orderId);
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(e, "HandleAsync nft order pay result fail, Id={Id}, Status={Status}", orderId, status);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "HandleAsync nft order pay result error, Id={Id}, Status={Status}", orderId, status);
            throw;
        }
    }
}