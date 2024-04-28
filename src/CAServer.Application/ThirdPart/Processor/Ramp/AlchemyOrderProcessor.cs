using System;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using CAServer.Signature.Provider;
using CAServer.ThirdPart.Alchemy;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Provider;
using CAServer.Tokens;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ThirdPart.Processor.Ramp;

[DisableAuditing]
public class AlchemyOrderProcessor : AbstractRampOrderProcessor
{
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly IOptionsMonitor<RampOptions> _rampOptions;
    private readonly AlchemyProvider _alchemyProvider;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly ISecretProvider _secretProvider;

    public AlchemyOrderProcessor(IClusterClient clusterClient,
        IThirdPartOrderProvider thirdPartOrderProvider,
        IDistributedEventBus distributedEventBus,
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions,
        AlchemyProvider alchemyProvider,
        IOrderStatusProvider orderStatusProvider, IAbpDistributedLock distributedLock, IBus broadcastBus,
        ISecretProvider secretProvider,
        ITokenAppService tokenAppService, IOptionsMonitor<ChainOptions> chainOptions,
        IOptionsMonitor<RampOptions> rampOptions) : base(
        clusterClient, thirdPartOrderProvider, distributedEventBus, orderStatusProvider, distributedLock, broadcastBus,
        tokenAppService, chainOptions)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _thirdPartOptions = thirdPartOptions;
        _alchemyProvider = alchemyProvider;
        _secretProvider = secretProvider;
        _rampOptions = rampOptions;
    }

    private AlchemyOrderUpdateDto ConvertToAlchemyOrder(IThirdPartOrder orderDto)
    {
        if (orderDto is not AlchemyOrderUpdateDto dto)
            throw new UserFriendlyException("not Alchemy-order");
        Logger.LogInformation("Alchemy order: {Order}", JsonConvert.SerializeObject(dto));
        return dto;
    }

    public override string ThirdPartName()
    {
        return ThirdPartNameType.Alchemy.ToString();
    }


    public string MappingToAlchemyNetwork(string network)
    {
        if (network.IsNullOrEmpty()) return network;
        var mappingExists = _rampOptions.CurrentValue.Provider(ThirdPartNameType.Alchemy).NetworkMapping
            .TryGetValue(network, out var mappingNetwork);
        return mappingExists ? mappingNetwork : network;
    }

    public string MappingFromAlchemyNetwork(string network)
    {
        if (network.IsNullOrEmpty()) return network;
        var mappingNetwork = _rampOptions.CurrentValue.Provider(ThirdPartNameType.Alchemy).NetworkMapping
            .FirstOrDefault(kv => kv.Value == network);
        return mappingNetwork.Key.DefaultIfEmpty(network);
    }

    public string MappingToAlchemySymbol(string symbol)
    {
        if (symbol.IsNullOrEmpty()) return symbol;
        var mappingExists = _rampOptions.CurrentValue.Provider(ThirdPartNameType.Alchemy).SymbolMapping
            .TryGetValue(symbol, out var achSymbol);
        return mappingExists ? achSymbol : symbol;
    }

    public string MappingFromAchSymbol(string symbol)
    {
        if (symbol.IsNullOrEmpty()) return symbol;
        var mappingNetwork = _rampOptions.CurrentValue.Provider(ThirdPartNameType.Alchemy).SymbolMapping
            .FirstOrDefault(kv => kv.Value == symbol);
        return mappingNetwork.Key.DefaultIfEmpty(symbol);
    }

    protected override async Task<OrderDto> VerifyOrderInputAsync<T>(T iThirdPartOrder)
    {
        var input = ConvertToAlchemyOrder(iThirdPartOrder);
        // verify signature of input
        var expectedBuySignature =
            await GetAlchemyBuySignatureAsync(input.OrderNo, input.Crypto, input.Network, input.Address);
        var expectedSellSignature =
            await GetAlchemySellSignatureAsync(input.OrderNo, input.Crypto, input.Network, input.Address);
        AssertHelper.IsTrue(input.Signature == expectedBuySignature || input.Signature == expectedSellSignature,
            "signature NOT match, input sign:{}", input.Signature);

        // convert input param to orderDto
        var orderDto = ObjectMapper.Map<AlchemyOrderUpdateDto, OrderDto>(input);

        // mapping ach-order-status to Ramp-order-status
        orderDto.Id = Guid.Parse(input.MerchantOrderNo);
        orderDto.MerchantName = ThirdPartName();
        orderDto.Status = AlchemyHelper.GetOrderStatus(orderDto.Status).ToString();
        orderDto.ThirdPartCrypto = input.Crypto;
        orderDto.ThirdPartNetwork = input.Network;
        orderDto.Network = MappingFromAlchemyNetwork(input.Network); 
        orderDto.Crypto = MappingFromAchSymbol(input.Crypto);
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
            orderPendingUpdate.Signature = await GetAlchemySellSignatureAsync(orderPendingUpdate.OrderNo,
                orderPendingUpdate.Crypto, 
                orderPendingUpdate.Network,
                orderPendingUpdate.Address);

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
            achOrder.MerchantName = ThirdPartName();
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

    private async Task<string> GetAlchemyBuySignatureAsync(string orderNo, string crypto, string network,
        string address)
    {
        var source = _thirdPartOptions.CurrentValue.Alchemy.AppId + orderNo + crypto + network + address;
        return await _secretProvider.GetAlchemyShaSignAsync(_thirdPartOptions.CurrentValue.Alchemy.AppId, source);
    }

    private async Task<string> GetAlchemySellSignatureAsync(string orderNo, string crypto, string network,
        string address)
    {
        var source = orderNo + crypto + network + address;
        return await _secretProvider.GetAlchemyShaSignAsync(_thirdPartOptions.CurrentValue.Alchemy.AppId, source);
    }
}