using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Http;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using Google.Protobuf.WellKnownTypes;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Provider;

public interface IOrderStatusProvider
{
    Task<OrderGrainDto> GetRampOrderAsync(Guid orderId);
    Task<NftOrderGrainDto> GetNftOrderAsync(Guid orderId);
    Task AddOrderStatusInfoAsync(OrderStatusInfoGrainDto grainDto);
    Task UpdateOrderStatusAsync(OrderStatusUpdateDto orderStatusDto);
    
    Task<CommonResponseDto<Empty>> UpdateOrderAsync(OrderDto orderDto, Dictionary<string, string> extension = null);
    Task<CommonResponseDto<Empty>> UpdateRampOrderAsync(OrderGrainDto dataToBeUpdated,
        Dictionary<string, string> extension = null);

    Task<CommonResponseDto<Empty>> UpdateNftOrderAsync(NftOrderGrainDto dataToBeUpdated);
    Task<int> CallBackNftOrderPayResultAsync(Guid orderId);
}

public class OrderStatusProvider : IOrderStatusProvider, ISingletonDependency
{
    private readonly ILogger<OrderStatusProvider> _logger; 
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly IHttpProvider _httpProvider;
    private readonly IBus _broadcastBus;

    private static readonly JsonSerializerSettings JsonSerializerSettings = JsonSettingsBuilder.New()
        .WithCamelCasePropertyNamesResolver()
        .WithAElfTypesConverters()
        .IgnoreNullValue().Build();


    public OrderStatusProvider(
        ILogger<OrderStatusProvider> logger,
        IThirdPartOrderProvider thirdPartOrderProvider,
        IObjectMapper objectMapper,
        IClusterClient clusterClient,
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions,
        IHttpProvider httpProvider,
        IDistributedEventBus distributedEventBus, IBus broadcastBus)
    {
        _logger = logger;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _thirdPartOptions = thirdPartOptions;
        _httpProvider = httpProvider;
        _distributedEventBus = distributedEventBus;
        _broadcastBus = broadcastBus;
    }

    public async Task<OrderGrainDto> GetRampOrderAsync(Guid orderId)
    {
        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(orderId);
        var orderGrainDto = await orderGrain.GetOrder();
        AssertHelper.IsTrue(orderGrainDto.Success, "Get NFT order failed.");
        return orderGrainDto.Data == null || orderGrainDto.Data.Id != orderId ? null : orderGrainDto.Data;
    }

    public async Task<NftOrderGrainDto> GetNftOrderAsync(Guid orderId)
    {
        var nftOrderGrain = _clusterClient.GetGrain<INftOrderGrain>(orderId);
        var nftOrderGrainDto = await nftOrderGrain.GetNftOrder();
        AssertHelper.IsTrue(nftOrderGrainDto.Success, "Get NFT order failed.");
        return nftOrderGrainDto.Data == null || nftOrderGrainDto.Data.Id != orderId ? null : nftOrderGrainDto.Data;
    }


    public async Task<CommonResponseDto<Empty>> UpdateOrderAsync(OrderDto orderDto, Dictionary<string, string> extension = null)
    {
        var existsOrderDto = await _thirdPartOrderProvider.GetThirdPartOrderAsync(orderDto.Id.ToString());
        AssertHelper.NotNull(existsOrderDto, "Order not found, id={Id}", orderDto.Id);
        AssertHelper.IsTrue(orderDto.Id == existsOrderDto.Id, "Order invalid");
        
        var dataToBeUpdated = MergeEsAndInput2GrainModel(orderDto, existsOrderDto);
        dataToBeUpdated.Status = orderDto.Status;
        dataToBeUpdated.Id = existsOrderDto.Id;
        dataToBeUpdated.UserId = existsOrderDto.UserId;
        dataToBeUpdated.LastModifyTime = TimeHelper.GetTimeStampInMilliseconds().ToString();
        _logger.LogInformation("This {MerchantName} order {GrainId} will be updated", orderDto.MerchantName,
            orderDto.Id);
        
        return await UpdateRampOrderAsync(dataToBeUpdated, extension);
    }
    
    
    private OrderGrainDto MergeEsAndInput2GrainModel(OrderDto fromData, OrderDto toData)
    {
        var orderGrainData = _objectMapper.Map<OrderDto, OrderGrainDto>(fromData);
        var orderData = _objectMapper.Map<OrderDto, OrderGrainDto>(toData);
        foreach (var prop in typeof(OrderGrainDto).GetProperties())
        {
            // When the attribute in UpdateOrderData has been assigned, there is no need to overwrite it with the data in es
            if (prop.GetValue(orderGrainData) == null && prop.GetValue(orderData) != null)
            {
                prop.SetValue(orderGrainData, prop.GetValue(orderData));
            }
        }

        return orderGrainData;
    }

    // update ramp order
    public async Task<CommonResponseDto<Empty>> UpdateRampOrderAsync(OrderGrainDto dataToBeUpdated,
        Dictionary<string, string> extension = null)
    {
        AssertHelper.NotEmpty(dataToBeUpdated.Id, "Update order id can not be empty");
        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(dataToBeUpdated.Id);
        _logger.LogInformation("This {ThirdPartName} order {OrderId} will be updated", dataToBeUpdated.MerchantName,
            dataToBeUpdated.Id);

        var result = await orderGrain.UpdateOrderAsync(dataToBeUpdated);
        AssertHelper.IsTrue(result.Success, "Update order error");

        var orderStatusGrainDto = _objectMapper.Map<OrderGrainDto, OrderStatusInfoGrainDto>(result.Data);
        orderStatusGrainDto.OrderStatusInfo.Extension =
            extension.IsNullOrEmpty() ? null : JsonConvert.SerializeObject(extension);
        await AddOrderStatusInfoAsync(orderStatusGrainDto);

        var orderChangeEto = _objectMapper.Map<OrderGrainDto, OrderEto>(result.Data);
        await _distributedEventBus.PublishAsync(orderChangeEto, false);
        await _broadcastBus.Publish(orderChangeEto);
        return new CommonResponseDto<Empty>();
    }

    // update NFT order
    public async Task<CommonResponseDto<Empty>> UpdateNftOrderAsync(NftOrderGrainDto dataToBeUpdated)
    {
        AssertHelper.NotEmpty(dataToBeUpdated.Id, "Update nft order id can not be empty");
        var nftOrderGrain = _clusterClient.GetGrain<INftOrderGrain>(dataToBeUpdated.Id);
        _logger.LogInformation("This {MerchantName} nft order {OrderId} will be updated", dataToBeUpdated.MerchantName,
            dataToBeUpdated.Id);

        var result = await nftOrderGrain.UpdateNftOrder(dataToBeUpdated);
        AssertHelper.IsTrue(result.Success, "Update nft order error");

        await _distributedEventBus.PublishAsync(new NftOrderEto(result.Data), false);
        return new CommonResponseDto<Empty>();
    }

    // call back NFT order pay result to Merchant webhookUrl
    public async Task<int> CallBackNftOrderPayResultAsync(Guid orderId)
    {
        var status = string.Empty;
        try
        {
            // query order grain

            var nftOrderGrainDto = await GetNftOrderAsync(orderId);
            AssertHelper.IsTrue(nftOrderGrainDto?.WebhookStatus != NftOrderWebhookStatus.SUCCESS.ToString(),
                "Webhook status of order {OrderId} exists", orderId);
            if (nftOrderGrainDto?.WebhookCount >= _thirdPartOptions.CurrentValue.Timer.NftCheckoutMerchantCallbackCount)
                return 0;

            var orderGrainDto = await GetRampOrderAsync(orderId);
            AssertHelper.NotEmpty(orderGrainDto.TransactionId, "Settlement transactionId missing.");
            status = orderGrainDto.Status;

            // callback merchant webhook API and fill raw result
            nftOrderGrainDto = await DoCallBackNftOrderPayResultAsync(status, nftOrderGrainDto);

            nftOrderGrainDto.WebhookTime = DateTime.UtcNow.ToUtcString();
            nftOrderGrainDto.WebhookCount++;

            var nftOrderResult = await UpdateNftOrderAsync(nftOrderGrainDto);
            AssertHelper.IsTrue(nftOrderResult.Success,
                "Webhook result update fail, webhookStatus={WebhookStatus}, webhookResult={WebhookResult}",
                nftOrderGrainDto.WebhookStatus, nftOrderGrainDto.WebhookResult);
            return 1;
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(e, "Handle nft order callback fail, Id={Id}, Status={Status}", orderId, status);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Handle nft order callback error, Id={Id}, Status={Status}", orderId, status);
        }

        return 0;
    }

    private async Task<NftOrderGrainDto> DoCallBackNftOrderPayResultAsync(string orderStatus,
        NftOrderGrainDto nftOrderGrainDto)
    {
        try
        {
            var requestDto = new NftOrderResultRequestDto
            {
                MerchantName = nftOrderGrainDto.MerchantName,
                MerchantOrderId = nftOrderGrainDto.MerchantOrderId,
                OrderId = nftOrderGrainDto.Id.ToString(),
                Status = orderStatus == OrderStatusType.Pending.ToString()
                    ? NftOrderWebhookStatus.SUCCESS.ToString()
                    : NftOrderWebhookStatus.FAIL.ToString()
            };
            _thirdPartOrderProvider.SignMerchantDto(requestDto);

            // do callback merchant
            var res = await _httpProvider.InvokeAsync(HttpMethod.Post, nftOrderGrainDto.WebhookUrl,
                body: JsonConvert.SerializeObject(requestDto, JsonSerializerSettings));
            nftOrderGrainDto.WebhookResult = res;

            var resObj = JsonConvert.DeserializeObject<CommonResponseDto<Empty>>(res);
            nftOrderGrainDto.WebhookStatus = resObj.Success
                ? NftOrderWebhookStatus.SUCCESS.ToString()
                : NftOrderWebhookStatus.FAIL.ToString();
        }
        catch (HttpRequestException e)
        {
            _logger.LogWarning(e, "Do callback nft order fail, Id={Id}, Status={Status}",
                nftOrderGrainDto.Id, orderStatus);
            nftOrderGrainDto.WebhookStatus = NftOrderWebhookStatus.FAIL.ToString();
            nftOrderGrainDto.WebhookResult = e.Message;
        }

        return nftOrderGrainDto;
    }

    public async Task UpdateOrderStatusAsync(OrderStatusUpdateDto orderStatusDto)
    {
        try
        {
            if (orderStatusDto.Order == null || orderStatusDto.Order.Id == Guid.Empty)
            {
                orderStatusDto.Order = await GetOrderAsync(orderStatusDto.OrderId);
            }

            var orderGrain = _clusterClient.GetGrain<IOrderGrain>(orderStatusDto.Order.Id);
            var getGrainResult = await orderGrain.GetOrder();
            if (!getGrainResult.Success)
            {
                _logger.LogError("Order {OrderId} is not existed in storage.", orderStatusDto.Order.Id);
                return;
            }

            var grainDto = getGrainResult.Data;
            grainDto.Status = orderStatusDto.Status.ToString();

            if (string.IsNullOrWhiteSpace(grainDto.TransactionId))
            {
                grainDto.TransactionId = orderStatusDto.Order.TransactionId;
            }

            grainDto.LastModifyTime = TimeHelper.GetTimeStampInMilliseconds().ToString();

            var result = await orderGrain.UpdateOrderAsync(grainDto);

            if (!result.Success)
            {
                _logger.LogError("Update user order fail, third part order number: {orderId}", orderStatusDto.Order.Id);
                return;
            }

            var orderEto = _objectMapper.Map<OrderGrainDto, OrderEto>(result.Data);
            await _distributedEventBus.PublishAsync(orderEto, false,
                false);

            var statusInfoDto = _objectMapper.Map<OrderGrainDto, OrderStatusInfoGrainDto>(result.Data);
            statusInfoDto.RawTransaction = orderStatusDto.RawTransaction;
            statusInfoDto.OrderStatusInfo.Extension =
                JsonConvert.SerializeObject(orderStatusDto.DicExt ?? new Dictionary<string, object>());

            await _broadcastBus.Publish(orderEto);
            await AddOrderStatusInfoAsync(statusInfoDto);
        }
        catch (Exception e)
        {
            var orderId = orderStatusDto.Order == null
                ? orderStatusDto.OrderId
                : orderStatusDto.Order.Id.ToString();

            _logger.LogError(e, "Update order status fail. orderId:{orderId}, status:{status}", orderId,
                orderStatusDto.Status);
        }
    }

    public async Task AddOrderStatusInfoAsync(OrderStatusInfoGrainDto grainDto)
    {
        var orderStatusGrain = _clusterClient.GetGrain<IOrderStatusInfoGrain>(
            GrainIdHelper.GenerateGrainId(CommonConstant.OrderStatusInfoPrefix, grainDto.OrderId.ToString("N")));
        var addResult = await orderStatusGrain.AddOrderStatusInfo(grainDto);
        if (addResult == null) return;

        var eto = _objectMapper.Map<OrderStatusInfoGrainResultDto, OrderStatusInfoEto>(addResult);
        await _distributedEventBus.PublishAsync(eto, false, false);
    }

    private async Task<OrderDto> GetOrderAsync(string orderId)
    {
        var orderData = await _thirdPartOrderProvider.GetThirdPartOrderAsync(orderId);

        if (string.IsNullOrEmpty(orderData.ThirdPartOrderNo) || string.IsNullOrEmpty(orderData.Id.ToString()) ||
            string.IsNullOrEmpty(orderData.TransDirect))
        {
            _logger.LogError("Order {OrderId} is not existed in storage.", orderId);
        }

        return orderData;
    }
}