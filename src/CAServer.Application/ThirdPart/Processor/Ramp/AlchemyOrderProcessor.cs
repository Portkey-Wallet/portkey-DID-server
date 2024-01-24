using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using CAServer.SecurityServer;
using CAServer.Signature.Provider;
using CAServer.ThirdPart.Alchemy;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Provider;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ThirdPart.Processor.Ramp;

public class AlchemyOrderProcessor : AbstractRampOrderProcessor
{
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly AlchemyProvider _alchemyProvider;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly ISecretProvider _secretProvider;

    public AlchemyOrderProcessor(IClusterClient clusterClient,
        IThirdPartOrderProvider thirdPartOrderProvider,
        IDistributedEventBus distributedEventBus,
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions,
        AlchemyProvider alchemyProvider,
        IOrderStatusProvider orderStatusProvider, IAbpDistributedLock distributedLock, IBus broadcastBus,
        ISecretProvider secretProvider) : base(
        clusterClient, thirdPartOrderProvider, distributedEventBus, orderStatusProvider, distributedLock, broadcastBus)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _thirdPartOptions = thirdPartOptions;
        _alchemyProvider = alchemyProvider;
        _secretProvider = secretProvider;
    }

    private AlchemyOrderUpdateDto ConvertToAlchemyOrder(IThirdPartOrder orderDto)
    {
        if (orderDto is not AlchemyOrderUpdateDto dto)
            throw new UserFriendlyException("not Alchemy-order");
        return dto;
    }

    public override string ThirdPartName()
    {
        return ThirdPartNameType.Alchemy.ToString();
    }

    protected override async Task<OrderDto> VerifyOrderInputAsync<T>(T iThirdPartOrder)
    {
        var input = ConvertToAlchemyOrder(iThirdPartOrder);
        // verify signature of input
        var expectedSignature = await GetAlchemySignatureAsync(input.OrderNo, input.Crypto, input.Network, input.Address);
        AssertHelper.IsTrue(input.Signature == expectedSignature, "signature NOT match, input sign:{}", input.Signature);

        // convert input param to orderDto
        var orderDto = ObjectMapper.Map<AlchemyOrderUpdateDto, OrderDto>(input);

        // mapping ach-order-status to Ramp-order-status
        orderDto.Id = Guid.Parse(input.MerchantOrderNo);
        orderDto.MerchantName = ThirdPartName();
        orderDto.Status = AlchemyHelper.GetOrderStatus(orderDto.Status).ToString();
        return orderDto;
    }

    public override async Task UpdateTxHashAsync(TransactionHashDto input)
    {
        try
        {
            Logger.LogInformation("UpdateAlchemyTxHash OrderId: {OrderId} TxHash:{TxHash} will send to alchemy",
                input.OrderId, input.TxHash);
            var orderId = ThirdPartHelper.GetOrderId(input.OrderId);
            var orderData = await _thirdPartOrderProvider.GetThirdPartOrderAsync(orderId.ToString());
            if (orderData == null)
            {
                Logger.LogError("No order found for {OrderId}", orderId);
                throw new UserFriendlyException($"No order found for {orderId}");
            }

            var orderPendingUpdate = ObjectMapper.Map<OrderDto, WaitToSendOrderInfoDto>(orderData);
            orderPendingUpdate.TxHash = input.TxHash;
            orderPendingUpdate.AppId = _thirdPartOptions.CurrentValue.Alchemy.AppId;
            orderPendingUpdate.Signature = await GetAlchemySignatureAsync(orderPendingUpdate.OrderNo, orderPendingUpdate.Crypto,
                orderPendingUpdate.Network, orderPendingUpdate.Address);

            await _alchemyProvider.UpdateOffRampOrderAsync(orderPendingUpdate);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Occurred error during update alchemy order transaction hash");
        }
    }

    public override async Task<OrderDto> QueryThirdOrderAsync(OrderDto orderDto)
    {
        try
        {
            var orderQueryDto = new QueryAlchemyOrderDto()
            {
                Side = AlchemyHelper.GetOrderTransDirectForQuery(orderDto.TransDirect),
                MerchantOrderNo = orderDto.Id.ToString(),
                OrderNo = orderDto.ThirdPartOrderNo
            };

            var queryResult = await _alchemyProvider.QueryAlchemyOrderInfoAsync(orderQueryDto);
            var achOrder = ObjectMapper.Map<QueryAlchemyOrderInfo, OrderDto>(queryResult);
            achOrder.Status = AlchemyHelper.GetOrderStatus(achOrder.Status).ToString();
            return achOrder;
        }
        catch (Exception e)
        {
            Logger.LogError(e,
                "Error deserializing query alchemy order info. orderId:{OrderId}, thirdPartOrderNo:{ThirdPartOrderNo}",
                orderDto.Id.ToString(), orderDto.ThirdPartOrderNo);
            return new OrderDto();
        }
    }

    private async Task<string> GetAlchemySignatureAsync(string orderNo, string crypto, string network, string address)
    {
        var source = orderNo + crypto + network + address;
        return await _secretProvider.GetAlchemyShaSignAsync(_thirdPartOptions.CurrentValue.Alchemy.AppId, source);
    }
}