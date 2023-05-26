using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Alchemy;

public class AlchemyOrderAppService : CAServerAppService, IAlchemyOrderAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<AlchemyOrderAppService> _logger;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    private readonly AlchemyOptions _alchemyOptions;
    private readonly IAlchemyProvider _alchemyProvider;

    public AlchemyOrderAppService(IClusterClient clusterClient,
        IThirdPartOrderProvider thirdPartOrderProvider,
        IDistributedEventBus distributedEventBus,
        ILogger<AlchemyOrderAppService> logger,
        IOptions<ThirdPartOptions> merchantOptions,
        IAlchemyProvider alchemyProvider,
        IObjectMapper objectMapper)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _distributedEventBus = distributedEventBus;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _logger = logger;
        _alchemyOptions = merchantOptions.Value.alchemy;
        _alchemyProvider = alchemyProvider;
    }

    public async Task<BasicOrderResult> UpdateAlchemyOrderAsync(AlchemyOrderUpdateDto input)
    {
        //callback signature format(appId,appSecret,appId+orderNo+crypto+network+address)
        var callbackSignature = $"{_alchemyOptions.AppId}{input.OrderNo}{input.Crypto}{input.Network}{input.Address}";
        if (!CheckSignature(input.Signature, callbackSignature))
        {
            _logger.LogWarning(
                "This callback verification failed, order id :{OrderNo}, and the signature :{Signature}",
                input.OrderNo, input.Signature);
            return new BasicOrderResult()
            {
                Message =
                    $"This callback verification failed, order id :{input.OrderNo}, and the signature is:{input.Signature}"
            };
        }

        Guid grainId = ThirdPartHelper.GetOrderId(input.MerchantOrderNo);
        var esOrderData = await _thirdPartOrderProvider.GetThirdPartOrderAsync(grainId.ToString());
        if (esOrderData == null || input.MerchantOrderNo != esOrderData.Id.ToString())
        {
            return new BasicOrderResult() { Message = $"No order found for {grainId}" };
        }

        if (esOrderData.Status == input.Status)
        {
            return new BasicOrderResult() { Message = $"Order status {input.Status} no need to update." };
        }

        var dataToBeUpdated = MergeEsAndInput2GrainModel(input, esOrderData);
        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(grainId);
        dataToBeUpdated.Status = AlchemyHelper.GetOrderStatus(input.Status);
        dataToBeUpdated.Id = grainId;
        dataToBeUpdated.UserId = esOrderData.UserId;
        dataToBeUpdated.LastModifyTime = TimeStampHelper.GetTimeStampInMilliseconds();
        _logger.LogDebug("This alchemy order {grainId} will be updated.", grainId);

        var result = await orderGrain.UpdateOrderAsync(dataToBeUpdated);

        if (!result.Success)
        {
            _logger.LogError("Update user order fail, third part order number: {orderId}", input.MerchantOrderNo);
            return new BasicOrderResult() { Message = $"Update order failed,{result.Message}" };
        }

        await _distributedEventBus.PublishAsync(_objectMapper.Map<OrderGrainDto, OrderEto>(result.Data));
        return new BasicOrderResult() { Success = true, Message = result.Message };
    }

    public async Task UpdateAlchemyTxHashAsync(UpdateAlchemyTxHashDto input)
    {
        Guid grainId = ThirdPartHelper.GetOrderId(input.OrderId);
        var orderData = await _thirdPartOrderProvider.GetThirdPartOrderAsync(grainId.ToString());
        if (orderData == null)
        {
            _logger.LogError("No order found for {grainId}", grainId);
            throw new UserFriendlyException($"No order found for {grainId}");
        }

        var alchemySellOrderWaitUpdated = _objectMapper.Map<OrderDto, UpdateAlchemySellOrderDto>(orderData);
        alchemySellOrderWaitUpdated.TxHash = input.TxHash;

        await _alchemyProvider.HttpPost2Alchemy(_alchemyOptions.UpdateSellOrderUri,
            JsonConvert.SerializeObject(alchemySellOrderWaitUpdated));
    }

    private OrderGrainDto MergeEsAndInput2GrainModel(AlchemyOrderUpdateDto alchemyData, OrderDto esOrderData)
    {
        var orderGrainData = _objectMapper.Map<AlchemyOrderUpdateDto, OrderGrainDto>(alchemyData);
        var orderData = _objectMapper.Map<OrderDto, OrderGrainDto>(esOrderData);
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

    private bool CheckSignature(string signature, string requestSignature)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(_alchemyOptions.AppId + _alchemyOptions.AppSecret + requestSignature);
        byte[] hashBytes = SHA1.Create().ComputeHash(bytes);

        StringBuilder sb = new StringBuilder();
        foreach (var t in hashBytes)
        {
            sb.Append(t.ToString("X2"));
        }

        var expectedSignature = sb.ToString().ToLower();
        _logger.LogDebug("Expected signature:{expectedSignature}", expectedSignature);
        return expectedSignature == signature;
    }
}