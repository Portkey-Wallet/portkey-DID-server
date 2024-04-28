using CAServer.Common;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Processors;
using CAServer.ThirdPart.Provider;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.BackGround.EventHandler;

public class NftOrderPaySuccessHandler : IDistributedEventHandler<OrderEto>, ITransientDependency
{
    private static readonly List<string> ResultStatus = new()
    {
        OrderStatusType.Pending.ToString(),
        OrderStatusType.TransferFailed.ToString()
    };

    private readonly ILogger<NftOrderPaySuccessHandler> _logger;
    private readonly IOrderStatusProvider _orderStatusProvider;

    public NftOrderPaySuccessHandler(ILogger<NftOrderPaySuccessHandler> logger, 
        OrderStatusProvider orderStatusProvider)
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
            var orderGrainDto = await _orderStatusProvider.GetRampOrderAsync(orderId);
            AssertHelper.NotNull(orderGrainDto, "Order {orderId} not found", orderId);
            orderGrainDto.Status = OrderStatusType.StartTransfer.ToString();
            var updateRes = await _orderStatusProvider.UpdateRampOrderAsync(orderGrainDto);
            AssertHelper.IsTrue(updateRes.Success, "Update order status failed, status={Status}", orderGrainDto.Status);
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