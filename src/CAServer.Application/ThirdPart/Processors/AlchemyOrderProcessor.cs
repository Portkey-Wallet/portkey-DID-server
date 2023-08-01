using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Logging;
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
    private readonly AlchemyOptions _alchemyOptions;
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
        AlchemyOptions alchemyOptions,
        IAlchemyProvider alchemyProvider,
        IOrderStatusProvider orderStatusProvider,
        IObjectMapper objectMapper) : base(clusterClient, logger, thirdPartOrderProvider, distributedEventBus,
        orderStatusProvider, objectMapper)
    {
        _logger = logger;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _alchemyOptions = alchemyOptions;
        _alchemyProvider = alchemyProvider;
        _objectMapper = objectMapper;
    }

    public override string MerchantName()
    {
        return "Alchemy";
    }

    protected override void VerifyOrderInput<T>(T orderDto)
    {
        if (orderDto is not AlchemyOrderUpdateDto)
            throw new UserFriendlyException("not Alchemy-order");
        AlchemyOrderUpdateDto input = orderDto as AlchemyOrderUpdateDto;
        if (input.Signature != GetAlchemySignature(input.OrderNo, input.Crypto, input.Network, input.Address))
            throw new UserFriendlyException("signature NOT match");
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
            orderPendingUpdate.AppId = _alchemyOptions.AppId;

            orderPendingUpdate.Signature = GetAlchemySignature(orderPendingUpdate.OrderNo, orderPendingUpdate.Crypto,
                orderPendingUpdate.Network, orderPendingUpdate.Address);

            await _alchemyProvider.HttpPost2AlchemyAsync(_alchemyOptions.UpdateSellOrderUri,
                JsonConvert.SerializeObject(orderPendingUpdate, Formatting.None, _setting));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Occurred error during update alchemy order transaction hash");
        }
    }

    public override Task<T> QueryThirdOrder<T>(T orderDto)
    {
        throw new NotImplementedException();
    }

    protected override Guid GetOrderId<T>(T orderDto)
    {
        if (orderDto is not AlchemyOrderUpdateDto)
            throw new UserFriendlyException("not Alchemy-order");
        AlchemyOrderUpdateDto input = orderDto as AlchemyOrderUpdateDto;
        return ThirdPartHelper.GetOrderId(input.MerchantOrderNo);
    }


    private string GetAlchemySignature(string orderNo, string crypto, string network, string address)
    {
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(_alchemyOptions.AppId + _alchemyOptions.AppSecret + orderNo + crypto +
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