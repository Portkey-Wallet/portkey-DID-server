using System;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Provider;
using Google.Protobuf.WellKnownTypes;
using MassTransit;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans;
using Volo.Abp;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ThirdPart.Processor;

public abstract class AbstractRampOrderProcessor : CAServerAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IOrderStatusProvider _orderStatusProvider;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IBus _broadcastBus;

    protected readonly JsonSerializerSettings JsonDecodeSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    protected AbstractRampOrderProcessor(IClusterClient clusterClient,
        IThirdPartOrderProvider thirdPartOrderProvider, IDistributedEventBus distributedEventBus,
        IOrderStatusProvider orderStatusProvider, IAbpDistributedLock distributedLock, IBus broadcastBus)
    {
        _clusterClient = clusterClient;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _distributedEventBus = distributedEventBus;
        _orderStatusProvider = orderStatusProvider;
        _distributedLock = distributedLock;
        _broadcastBus = broadcastBus;
    }

    /// <summary>
    ///     Verify/Convert/Decode from ThirdPart-order-data to a verified order-data
    /// </summary>
    /// <param name="iThirdPartOrder"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    protected abstract Task<OrderDto> VerifyOrderInputAsync<T>(T iThirdPartOrder) where T : IThirdPartOrder;

    /// <summary>
    ///     Off-ramp, to notify thirdPart transfer transaction hash
    /// </summary>
    /// <param name="transactionHashDto"></param>
    /// <returns></returns>
    public virtual Task UpdateTxHashAsync(TransactionHashDto transactionHashDto)
    {
        // default do nothing
        return Task.CompletedTask;
    }

    /// <summary>
    ///     ThirdPart name
    /// </summary>
    /// <returns></returns>
    public abstract string ThirdPartName();

    /// <summary>
    ///     Query new Third order data, and convert to orderDto
    /// </summary>
    /// <param name="orderDto"></param>
    /// <returns></returns>
    public abstract Task<OrderDto> QueryThirdOrderAsync(OrderDto orderDto);

    
    public async Task<CommonResponseDto<Empty>> OrderUpdateAsync(IThirdPartOrder thirdPartOrder)
    {
        OrderDto inputOrderDto = null;
        try
        {
            inputOrderDto = await VerifyOrderInputAsync(thirdPartOrder);
            var grainId = inputOrderDto.Id;
            AssertHelper.NotEmpty(grainId, "Order id empty");
            await using var handle =
                await _distributedLock.TryAcquireAsync(name: "ramp:orderUpdate:" + grainId);
            AssertHelper.NotNull(handle, "Order update processing ABORT, orderId={OrderId}", grainId.ToString());

            var inputState = ThirdPartHelper.ParseOrderStatus(inputOrderDto.Status);
            AssertHelper.IsTrue(inputState != OrderStatusType.Unknown, "Unknown order status {Status}",
                inputOrderDto.Status);

            var orderGrain = _clusterClient.GetGrain<IOrderGrain>(grainId);
            var orderDataResp = await orderGrain.GetOrder();
            AssertHelper.NotNull(orderDataResp.Success, "Order not found, id={Id}", grainId);
            AssertHelper.NotNull(orderDataResp.Data, "Order empty, id={Id}", grainId);
            AssertHelper.IsTrue(inputOrderDto.Id == orderDataResp.Data.Id, "Order invalid");
            var orderData = orderDataResp.Data;

            var currentStatus = ThirdPartHelper.ParseOrderStatus(orderData.Status);
            AssertHelper.IsTrue(OrderStatusTransitions.Reachable(currentStatus, inputState),
                "{ToState} isn't reachable from {FromState}", inputState, currentStatus);

            var inputOrder = ObjectMapper.Map<OrderDto, OrderGrainDto>(inputOrderDto);
            var dataToBeUpdated = MergeEsAndInput2GrainModel(inputOrder, orderData);
            dataToBeUpdated.Status = inputState.ToString(); 
            dataToBeUpdated.Id = grainId;
            dataToBeUpdated.UserId = orderData.UserId;
            dataToBeUpdated.LastModifyTime = TimeHelper.GetTimeStampInMilliseconds().ToString();
            Logger.LogInformation("This {MerchantName} order {GrainId} will be updated", inputOrderDto.MerchantName,
                grainId);

            var result = await orderGrain.UpdateOrderAsync(dataToBeUpdated);
            
            AssertHelper.IsTrue(result.Success, "Update order failed,{Message}", result.Message);

            var orderEto = ObjectMapper.Map<OrderGrainDto, OrderEto>(result.Data);
            await _distributedEventBus.PublishAsync(orderEto);
            await _orderStatusProvider.AddOrderStatusInfoAsync(
                ObjectMapper.Map<OrderGrainDto, OrderStatusInfoGrainDto>(result.Data));
            await _broadcastBus.Publish(orderEto);
            return new CommonResponseDto<Empty>();
        }
        catch (UserFriendlyException e)
        {
            Logger.LogWarning("Order update FAILED, {MerchantName}-{OrderId}-{ThirdPartOrderNo}",
                inputOrderDto?.MerchantName,
                inputOrderDto?.Id, inputOrderDto?.ThirdPartOrderNo);
            return new CommonResponseDto<Empty>().Error(e);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Order update ERROR, {MerchantName}-{OrderId}-{ThirdPartOrderNo}",
                inputOrderDto?.MerchantName,
                inputOrderDto?.Id, inputOrderDto?.ThirdPartOrderNo);
            return new CommonResponseDto<Empty>().Error(e, "INTERNAL ERROR, please try again later.");
        }
    }
    
    private OrderGrainDto MergeEsAndInput2GrainModel(OrderGrainDto fromData, OrderGrainDto toData)
    {
        foreach (var prop in typeof(OrderGrainDto).GetProperties())
        {
            // When the attribute in UpdateOrderData has been assigned, there is no need to overwrite it with the data in es
            if (prop.GetValue(fromData) == null && prop.GetValue(toData) != null)
            {
                prop.SetValue(fromData, prop.GetValue(toData));
            }
        }

        return fromData;
    }
}