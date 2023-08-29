using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Provider;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core.ThirdPart;

public class NftOrderPayResultHandler : IDistributedEventHandler<OrderEto>
{
    private static readonly List<string> ResultStatus = new()
    {
        OrderStatusType.Pending.ToString(),
        OrderStatusType.Failed.ToString(),
        OrderStatusType.Expired.ToString()
    };

    private readonly ILogger<NftOrderPayResultHandler> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly HttpProvider _httpProvider;
    private readonly IThirdPartOrderAppService _thirdPartOrderAppService;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;

    public NftOrderPayResultHandler(HttpProvider httpProvider, IClusterClient clusterClient,
        ILogger<NftOrderPayResultHandler> logger, IThirdPartOrderAppService thirdPartOrderAppService,
        IThirdPartOrderProvider thirdPartOrderProvider)
    {
        _httpProvider = httpProvider;
        _clusterClient = clusterClient;
        _logger = logger;
        _thirdPartOrderAppService = thirdPartOrderAppService;
        _thirdPartOrderProvider = thirdPartOrderProvider;
    }


    public async Task HandleEventAsync(OrderEto eventData)
    {
        if (!ResultStatus.Contains(eventData.Status) || eventData.TransDirect != TransferDirectionType.NFTBuy.ToString())
        {
            // not NFT pay result
            return;
        }

        try
        {
            // query base order grain 
            var orderGrain = _clusterClient.GetGrain<IOrderGrain>(eventData.Id);
            var orderGrainDto = (await orderGrain.GetOrder()).Data;
            AssertHelper.IsTrue(orderGrainDto?.Id == eventData.Id, "Order {OrderId} not exists", eventData.Id);

            if (orderGrainDto?.Status == OrderStatusType.Pending.ToString())
            {
                // pay success new status is StartTransfer
                orderGrainDto.Status = OrderStatusType.StartTransfer.ToString();
                var orderGrainResult = await _thirdPartOrderProvider.DoUpdateRampOrderAsync(orderGrainDto);
                AssertHelper.IsTrue(orderGrainResult.Success, 
                    "Order status update fail, OrderId={OrderId}, Status={Status}", 
                    orderGrainDto.Id, orderGrainDto.Status);
            }

            // query nft order grain
            var nftOrderGrain = _clusterClient.GetGrain<INftOrderGrain>(eventData.Id);
            var nftOrderGrainDto = await nftOrderGrain.GetNftOrder();
            AssertHelper.IsTrue(nftOrderGrainDto?.Data?.WebhookStatus == NftOrderWebhookStatus.NONE.ToString(),
                "Webhook status of order {OrderId} exists", orderGrainDto.Id);

            // callback and update result
            var grainDto = await DoCallBack(orderGrainDto, nftOrderGrainDto?.Data);
            var nftOrderResult = await _thirdPartOrderProvider.DoUpdateNftOrderAsync(grainDto);
            AssertHelper.IsTrue(nftOrderResult.Success,
                "Webhook result update fail, webhookStatus={WebhookStatus}, webhookResult={WebhookResult}",
                grainDto.WebhookStatus, grainDto.WebhookResult);
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(e, "Handle nft order pay result fail, Id={Id}, Status={Status}",
                eventData.Id, eventData.Status);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Handle nft order pay result error, Id={Id}, Status={Status}",
                eventData.Id, eventData.Status);
            throw;
        }
    }


    private async Task<NftOrderGrainDto> DoCallBack(OrderGrainDto orderGrainDto, NftOrderGrainDto nftOrderGrainDto)
    {
        try
        {
            var requestDto = new NftOrderResultRequestDto
            {
                MerchantName = nftOrderGrainDto.MerchantName,
                MerchantOrderId = nftOrderGrainDto.MerchantOrderId,
                OrderId = orderGrainDto.Id.ToString(),
                Status = orderGrainDto.Status == OrderStatusType.Pending.ToString()
                    ? NftOrderWebhookStatus.SUCCESS.ToString()
                    : NftOrderWebhookStatus.FAIL.ToString()
            };
            // sign data
            _thirdPartOrderProvider.SignMerchantDto(requestDto);

            // do callback
            var res = await _httpProvider.Invoke(HttpMethod.Post, nftOrderGrainDto.WebhookUrl,
                body: JsonConvert.SerializeObject(requestDto, HttpProvider.DefaultJsonSettings));
            nftOrderGrainDto.WebhookResult = res;

            var resObj = JsonConvert.DeserializeObject<CommonResponseDto<Empty>>(res);
            nftOrderGrainDto.WebhookStatus = resObj.Success
                ? NftOrderWebhookStatus.SUCCESS.ToString()
                : NftOrderWebhookStatus.FAIL.ToString();
        }
        catch (HttpRequestException e)
        {
            _logger.LogWarning(e, "Callback nft order pay result fail, Id={Id}, Status={Status}",
                nftOrderGrainDto.Id, orderGrainDto.Status);
            nftOrderGrainDto.WebhookStatus = NftOrderWebhookStatus.FAIL.ToString();
            nftOrderGrainDto.WebhookResult = e.Message;
        }

        nftOrderGrainDto.WebhookTime = DateTime.UtcNow.ToUtcString();
        nftOrderGrainDto.WebhookCount++;
        return nftOrderGrainDto;
    }
}