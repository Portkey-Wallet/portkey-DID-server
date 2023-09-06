using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.Grains;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Order;
using CAServer.ThirdPart.Etos;
using Google.Protobuf.WellKnownTypes;
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
    Task AddOrderStatusInfoAsync(OrderStatusInfoGrainDto grainDto);
    Task UpdateOrderStatusAsync(OrderStatusUpdateDto orderStatusDto);
    Task<CommonResponseDto<Empty>> UpdateRampOrderAsync(OrderGrainDto dataToBeUpdated);
    Task<CommonResponseDto<Empty>> UpdateNftOrderAsync(NftOrderGrainDto dataToBeUpdated);
    Task<int> CallBackNftOrderPayResultAsync(Guid orderId);
    void RegisterOrderChangeListener(string orderId, Func<NotifyOrderDto, Task> onOrderChanged);
    void RemoveOrderChangeListener(string orderId);
}

public class OrderStatusProvider : IOrderStatusProvider, ISingletonDependency
{
    private readonly ILogger<OrderStatusProvider> _logger;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ThirdPartOptions _thirdPartOptions;
    private readonly IHttpProvider _httpProvider;

    private readonly Dictionary<string, Func<NotifyOrderDto, Task>> _orderListeners = new();

    public OrderStatusProvider(
        ILogger<OrderStatusProvider> logger, 
        IThirdPartOrderProvider thirdPartOrderProvider,
        IObjectMapper objectMapper, 
        IClusterClient clusterClient, 
        IOptions<ThirdPartOptions> thirdPartOptions, 
        IHttpProvider httpProvider, 
        IDistributedEventBus distributedEventBus)
    {
        _logger = logger;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _thirdPartOptions = thirdPartOptions.Value;
        _httpProvider = httpProvider;
        _distributedEventBus = distributedEventBus;
    }

    public void RegisterOrderChangeListener(string orderId, Func<NotifyOrderDto, Task> onOrderChanged)
    {
        _orderListeners[orderId] = onOrderChanged;
    }
    
    public void RemoveOrderChangeListener(string orderId)
    {
        _orderListeners.Remove(orderId);
    }

    private void OnOrderChange(OrderGrainDto orderGrainDto)
    {
        if (!_orderListeners.TryGetValue(orderGrainDto.Id.ToString(), out var callbackFuc))
            return;
        try
        { 
            var notifyOrderDto = _objectMapper.Map<OrderGrainDto, NotifyOrderDto>(orderGrainDto);
            callbackFuc.Invoke(notifyOrderDto);
        }
        catch (Exception e)
        {
            _logger.LogError(e , "OnOrderChange call back error");
        }
    }
    
    
    // update ramp order
    public async Task<CommonResponseDto<Empty>> UpdateRampOrderAsync(OrderGrainDto dataToBeUpdated)
    {
        AssertHelper.NotEmpty(dataToBeUpdated.Id, "Update order id can not be empty");
        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(dataToBeUpdated.Id);
        dataToBeUpdated.LastModifyTime = TimeHelper.GetTimeStampInMilliseconds().ToString();
        _logger.LogInformation("This {ThirdPartName} order {OrderId} will be updated", dataToBeUpdated.MerchantName,
            dataToBeUpdated.Id);

        var result = await orderGrain.UpdateOrderAsync(dataToBeUpdated);
        AssertHelper.IsTrue(result.Success, "Update order error");

        await AddOrderStatusInfoAsync(
            _objectMapper.Map<OrderGrainDto, OrderStatusInfoGrainDto>(result.Data));
        
        // notify listener
        OnOrderChange(result.Data);
        
        
        await _distributedEventBus.PublishAsync(_objectMapper.Map<OrderGrainDto, OrderEto>(result.Data), false);
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

        
        await _distributedEventBus.PublishAsync(_objectMapper.Map<NftOrderGrainDto, NftOrderEto>(result.Data), false);
        return new CommonResponseDto<Empty>();
    }

    // call back NFT order pay result to Merchant webhookUrl
    public async Task<int> CallBackNftOrderPayResultAsync(Guid orderId)
    {
        var status = string.Empty;
        try
        {
            // query nft order grain
            var nftOrderGrain = _clusterClient.GetGrain<INftOrderGrain>(orderId);
            var nftOrderGrainDto = await nftOrderGrain.GetNftOrder();
            AssertHelper.IsTrue(nftOrderGrainDto?.Data?.WebhookStatus != NftOrderWebhookStatus.SUCCESS.ToString(),
                "Webhook status of order {OrderId} exists", orderId);
            if (nftOrderGrainDto?.Data?.WebhookCount >= _thirdPartOptions.Timer.NftCheckoutMerchantCallbackCount)
                return 0;

            var orderGrain = _clusterClient.GetGrain<IOrderGrain>(orderId);
            var orderGrainDto = await orderGrain.GetOrder();
            AssertHelper.IsTrue(orderGrainDto.Success, "Order {orderId} not exits.", orderId);
            status = orderGrainDto.Data.Status;
            
            // callback merchant and update result
            var grainDto = await DoCallBackNftOrderPayResultAsync(status, nftOrderGrainDto?.Data);
            
            grainDto.WebhookTime = DateTime.UtcNow.ToUtcString();
            grainDto.WebhookCount++;
            
            var nftOrderResult = await UpdateNftOrderAsync(grainDto);
            AssertHelper.IsTrue(nftOrderResult.Success,
                "Webhook result update fail, webhookStatus={WebhookStatus}, webhookResult={WebhookResult}",
                grainDto.WebhookStatus, grainDto.WebhookResult);
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

    private async Task<NftOrderGrainDto> DoCallBackNftOrderPayResultAsync(string orderStatus, NftOrderGrainDto nftOrderGrainDto)
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

            await _distributedEventBus.PublishAsync(_objectMapper.Map<OrderGrainDto, OrderEto>(result.Data), false,
                false);

            var statusInfoDto = _objectMapper.Map<OrderGrainDto, OrderStatusInfoGrainDto>(result.Data);
            statusInfoDto.RawTransaction = orderStatusDto.RawTransaction; 
            statusInfoDto.OrderStatusInfo.Extension =
                JsonConvert.SerializeObject(orderStatusDto.DicExt ?? new Dictionary<string, object>());

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