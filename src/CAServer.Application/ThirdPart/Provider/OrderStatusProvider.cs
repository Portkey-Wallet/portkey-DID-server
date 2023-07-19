using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Provider;

public interface IOrderStatusProvider
{
    Task AddOrderStatusInfoAsync(OrderStatusInfoGrainDto grainDto);
    Task UpdateOrderStatusAsync(OrderStatusUpdateDto orderStatusDto);
}

public class OrderStatusProvider : IOrderStatusProvider, ISingletonDependency
{
    private readonly ILogger<OrderStatusProvider> _logger;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;

    public OrderStatusProvider(ILogger<OrderStatusProvider> logger, IThirdPartOrderProvider thirdPartOrderProvider,
        IObjectMapper objectMapper, IClusterClient clusterClient, IDistributedEventBus distributedEventBus)
    {
        _logger = logger;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
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
            grainDto.TransactionId = orderStatusDto.Order.TransactionId;
            grainDto.LastModifyTime = TimeStampHelper.GetTimeStampInMilliseconds();

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