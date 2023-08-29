using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons.Dtos;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
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

    public NftOrderPayResultHandler(HttpProvider httpProvider, IClusterClient clusterClient, ILogger<NftOrderPayResultHandler> logger, IThirdPartOrderAppService thirdPartOrderAppService)
    {
        _httpProvider = httpProvider;
        _clusterClient = clusterClient;
        _logger = logger;
        _thirdPartOrderAppService = thirdPartOrderAppService;
    }


    public async Task HandleEventAsync(OrderEto eventData)
    {
        if (!ResultStatus.Contains(eventData.Status))
        {
            return;
        }

        try
        {
            // query nft order grain
            var nftOrderGrain = _clusterClient.GetGrain<INftOrderGrain>(eventData.Id);
            var nftOrderGrainDto = await nftOrderGrain.GetNftOrder();
            AssertHelper.IsTrue(nftOrderGrainDto?.Data?.WebhookStatus == NftOrderWebhookStatus.NONE.ToString(),
                "Webhook status of order {OrderId} exists", eventData.Id);
            
            // callback and update result
            var grainDto = await DoCallBack(eventData, nftOrderGrainDto?.Data);
            var updateResult = await nftOrderGrain.UpdateNftOrder(grainDto);
            AssertHelper.IsTrue(updateResult.Success, 
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


    private async Task<NftOrderGrainDto> DoCallBack(OrderEto eventData, NftOrderGrainDto nftOrderGrainDto)
    {
        try
        {
            var requestDto = new NftOrderResultRequestDto
            {
                MerchantName = nftOrderGrainDto.MerchantName,
                MerchantOrderId = nftOrderGrainDto.MerchantOrderId,
                OrderId = eventData.Id.ToString(),
                Status = eventData.Status == OrderStatusType.Pending.ToString()
                    ? NftOrderWebhookStatus.SUCCESS.ToString()
                    : NftOrderWebhookStatus.FAIL.ToString()
            };
            // sign data
            _thirdPartOrderAppService.SignMerchantDto(requestDto);

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
                nftOrderGrainDto.Id, eventData.Status);
            nftOrderGrainDto.WebhookStatus = NftOrderWebhookStatus.FAIL.ToString();
            nftOrderGrainDto.WebhookResult = e.Message;
        }

        nftOrderGrainDto.WebhookTime = DateTime.UtcNow.ToString("o");
        nftOrderGrainDto.WebhookCount++;
        return nftOrderGrainDto;
    }
    
}