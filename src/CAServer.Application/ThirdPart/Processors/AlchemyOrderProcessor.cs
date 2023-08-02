using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Processors;

public class AlchemyOrderProcessor : AbstractOrderProcessor
{
    private readonly ILogger<AbstractOrderProcessor> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly ThirdPartOptions _thirdPartOptions;
    private readonly IAlchemyProvider _alchemyProvider;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;

    private readonly JsonSerializerSettings _setting = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public AlchemyOrderProcessor(IClusterClient clusterClient,
        ILogger<AlchemyOrderProcessor> logger,
        IThirdPartOrderProvider thirdPartOrderProvider,
        IDistributedEventBus distributedEventBus,
        IOptions<ThirdPartOptions> thirdPartOptions,
        IAlchemyProvider alchemyProvider,
        IOrderStatusProvider orderStatusProvider,
        IObjectMapper objectMapper) : base(clusterClient, logger, thirdPartOrderProvider, distributedEventBus,
        orderStatusProvider, objectMapper)
    {
        _logger = logger;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _thirdPartOptions = thirdPartOptions.Value;
        _alchemyProvider = alchemyProvider;
        _objectMapper = objectMapper;
    }

    private AlchemyOrderUpdateDto ConvertToAlchemyOrder(IThirdPartOrder orderDto)
    {
        if (orderDto is not AlchemyOrderUpdateDto dto)
            throw new UserFriendlyException("not Alchemy-order");
        return dto;
    }

    public override string MerchantName()
    {
        return MerchantNameType.Alchemy.ToString();
    }

    protected override OrderDto ConvertOrderDto<T>(T iThirdPartOrder)
    {
        var aclOrder = ConvertToAlchemyOrder(iThirdPartOrder);
        return _objectMapper.Map<QueryAlchemyOrderInfo, OrderDto>(aclOrder);
    }

    public override string MapperOrderStatus(OrderDto orderDto)
    {
        return AlchemyHelper.GetOrderStatus(orderDto.Status).ToString();
    }

    protected override IThirdPartOrder VerifyOrderInput<T>(T iThirdPartOrder)
    {
        var input = ConvertToAlchemyOrder(iThirdPartOrder);
        var expectedSignature = GetAlchemySignature(input.OrderNo, input.Crypto, input.Network, input.Address); 
        if (input.Signature != expectedSignature)
            throw new UserFriendlyException("signature NOT match");
        return input;
    }

    public override async Task UpdateTxHashAsync(TransactionHashDto input)
    {
        try
        {
            _logger.LogInformation("UpdateAlchemyTxHash OrderId: {OrderId} TxHash:{TxHash} will send to alchemy",
                input.OrderId, input.TxHash);
            var orderId = ThirdPartHelper.GetOrderId(input.OrderId);
            var orderData = await _thirdPartOrderProvider.GetThirdPartOrderAsync(orderId.ToString());
            if (orderData == null)
            {
                _logger.LogError("No order found for {OrderId}", orderId);
                throw new UserFriendlyException($"No order found for {orderId}");
            }

            var orderPendingUpdate = _objectMapper.Map<OrderDto, WaitToSendOrderInfoDto>(orderData);
            orderPendingUpdate.TxHash = input.TxHash;
            orderPendingUpdate.AppId = _thirdPartOptions.alchemy.AppId;

            orderPendingUpdate.Signature = GetAlchemySignature(orderPendingUpdate.OrderNo, orderPendingUpdate.Crypto,
                orderPendingUpdate.Network, orderPendingUpdate.Address);

            await _alchemyProvider.HttpPost2AlchemyAsync(_thirdPartOptions.alchemy.UpdateSellOrderUri,
                JsonConvert.SerializeObject(orderPendingUpdate, Formatting.None, _setting));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Occurred error during update alchemy order transaction hash");
        }
    }

    public override async Task<OrderDto> QueryThirdOrder(OrderDto orderDto)
    {
        try
        {
            var orderQueryDto = new OrderQueryDto()
            {
                Side = AlchemyHelper.GetOrderTransDirectForQuery(orderDto.TransDirect),
                MerchantOrderNo = orderDto.Id.ToString(),
                OrderNo = orderDto.ThirdPartOrderNo
            };

            var queryString = string.Join("&", orderQueryDto.GetType().GetProperties()
                .Select(p => $"{char.ToLower(p.Name[0]) + p.Name.Substring(1)}={p.GetValue(orderQueryDto)}"));

            var queryResult = JsonConvert.DeserializeObject<QueryAlchemyOrderInfoResultDto>(
                await _alchemyProvider.HttpGetFromAlchemy(_thirdPartOptions.alchemy.MerchantQueryTradeUri + "?" +
                                                          queryString));

            return _objectMapper.Map<QueryAlchemyOrderInfo, OrderDto>(queryResult.Data);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "Error deserializing query alchemy order info. orderId:{OrderId}, thirdPartOrderNo:{ThirdPartOrderNo}",
                orderDto.Id.ToString(), orderDto.ThirdPartOrderNo);
            return new OrderDto();
        }
    }

    protected override Guid GenerateGrainId(OrderDto input)
    {
        return ThirdPartHelper.GetOrderId(input.Id.ToString());
    }


    private string GetAlchemySignature(string orderNo, string crypto, string network, string address)
    {
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(_thirdPartOptions.alchemy.AppId +
                                                  _thirdPartOptions.alchemy.AppSecret + orderNo + crypto +
                                                  network + address);
            byte[] hashBytes = SHA1.Create().ComputeHash(bytes);

            StringBuilder sb = new StringBuilder();
            foreach (var t in hashBytes)
            {
                sb.Append(t.ToString("X2"));
            }

            _logger.LogDebug("Generate Alchemy sell order signature successfully. Signature: {signature}",
                sb.ToString().ToLower());
            return sb.ToString().ToLower();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Generator alchemy update txHash signature failed, OrderNo: {orderNo}.", orderNo);
            return "";
        }
    }
}