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
        OrderStatusType.Transferred.ToString(),
    };

    private readonly ILogger<NftOrderMerchantCallbackHandler> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IOrderStatusProvider _orderStatusProvider;

    public NftOrderMerchantCallbackHandler(IClusterClient clusterClient,
        ILogger<NftOrderMerchantCallbackHandler> logger,
        IThirdPartOrderProvider thirdPartOrderProvider, IOrderStatusProvider orderStatusProvider)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _thirdPartOrderProvider = thirdPartOrderProvider;
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
            _logger.LogWarning(e, "Handle nft order pay result fail, Id={Id}, Status={Status}", orderId, status);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Handle nft order pay result error, Id={Id}, Status={Status}", orderId, status);
            throw;
        }
    }
}