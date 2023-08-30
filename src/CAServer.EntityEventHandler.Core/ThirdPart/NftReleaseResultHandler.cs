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
using CAServer.ThirdPart.Processors;
using CAServer.ThirdPart.Provider;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core.ThirdPart;

public class NftReleaseResultHandler : IDistributedEventHandler<OrderEto>
{
    private static readonly List<string> NftReleaseResultStatus = new()
    {
        OrderStatusType.Finish.ToString(),
        OrderStatusType.TransferFailed.ToString(),
    };

    private readonly ILogger<NftReleaseResultHandler> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly HttpProvider _httpProvider;
    private readonly IThirdPartOrderAppService _thirdPartOrderAppService;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IThirdPartOrderProcessorFactory _thirdPartOrderProcessorFactory;

    public NftReleaseResultHandler(HttpProvider httpProvider, IClusterClient clusterClient,
        ILogger<NftReleaseResultHandler> logger, IThirdPartOrderAppService thirdPartOrderAppService,
        IThirdPartOrderProvider thirdPartOrderProvider, IThirdPartOrderProcessorFactory thirdPartOrderProcessorFactory)
    {
        _httpProvider = httpProvider;
        _clusterClient = clusterClient;
        _logger = logger;
        _thirdPartOrderAppService = thirdPartOrderAppService;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _thirdPartOrderProcessorFactory = thirdPartOrderProcessorFactory;
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
            await _thirdPartOrderProcessorFactory.GetProcessor(eventData.MerchantName)
                .NotifyNftReleaseAsync(eventData.Id);
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
            _thirdPartOrderProvider.SignMerchantDto(requestDto);

            // do callback merchant
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