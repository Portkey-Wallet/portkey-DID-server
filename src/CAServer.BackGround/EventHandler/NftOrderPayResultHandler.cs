using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core.ThirdPart;

public class NftOrderPayResultHandler : IDistributedEventHandler<OrderEto>, ITransientDependency
{
    private static readonly List<string> ResultStatus = new()
    {
        OrderStatusType.Pending.ToString(),
        OrderStatusType.Failed.ToString(),
        OrderStatusType.Expired.ToString()
    };

    private readonly ILogger<NftOrderPayResultHandler> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IOrderStatusProvider _orderStatusProvider;

    public NftOrderPayResultHandler(IClusterClient clusterClient,
        ILogger<NftOrderPayResultHandler> logger,
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
            // query base order grain 
            var orderGrain = _clusterClient.GetGrain<IOrderGrain>(orderId);
            var orderGrainDto = (await orderGrain.GetOrder()).Data;
            AssertHelper.IsTrue(orderGrainDto?.Id == orderId, "Order {OrderId} not exists", orderId);

            // The order status should only change when the event's order status is 'pay-success'. 
            if (orderGrainDto?.Status == OrderStatusType.Pending.ToString())
            {
                // order's next status should be StartTransfer
                orderGrainDto.Status = OrderStatusType.StartTransfer.ToString();
                var orderGrainResult = await _orderStatusProvider.UpdateRampOrderAsync(orderGrainDto);
                AssertHelper.IsTrue(orderGrainResult.Success,
                    "Order status update fail, OrderId={OrderId}, Status={Status}",
                    orderGrainDto.Id, orderGrainDto.Status);
            }

            // query nft order grain
            var nftOrderGrain = _clusterClient.GetGrain<INftOrderGrain>(orderId);
            var nftOrderGrainDto = await nftOrderGrain.GetNftOrder();
            AssertHelper.IsTrue(nftOrderGrainDto?.Data?.WebhookStatus == NftOrderWebhookStatus.NONE.ToString(),
                "Webhook status of order {OrderId} exists", orderId);

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