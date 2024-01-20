using System;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Provider;
using CAServer.Tokens;
using Google.Protobuf.WellKnownTypes;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans;
using Volo.Abp;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ThirdPart.Processor;

public abstract class AbstractRampOrderProcessor : CAServerAppService
{
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IOrderStatusProvider _orderStatusProvider;
    private readonly IAbpDistributedLock _distributedLock;

    protected readonly JsonSerializerSettings JsonDecodeSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    protected AbstractRampOrderProcessor(IClusterClient clusterClient,
        IThirdPartOrderProvider thirdPartOrderProvider, IDistributedEventBus distributedEventBus,
        IOrderStatusProvider orderStatusProvider, IAbpDistributedLock distributedLock, IBus broadcastBus,
        ITokenAppService tokenAppService, IOptionsMonitor<ChainOptions> chainOptions)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _orderStatusProvider = orderStatusProvider;
        _distributedLock = distributedLock;
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

            var esOrderData = await _thirdPartOrderProvider.GetThirdPartOrderAsync(grainId.ToString());
            AssertHelper.NotNull(esOrderData, "Order not found, id={Id}", grainId);
            AssertHelper.IsTrue(inputOrderDto.Id == esOrderData.Id, "Order invalid");

            var currentStatus = ThirdPartHelper.ParseOrderStatus(esOrderData.Status);
            AssertHelper.IsTrue(OrderStatusTransitions.Reachable(currentStatus, inputState),
                "{ToState} isn't reachable from {FromState}", inputState, currentStatus);

            return await _orderStatusProvider.UpdateOrderAsync(inputOrderDto);
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
    
}