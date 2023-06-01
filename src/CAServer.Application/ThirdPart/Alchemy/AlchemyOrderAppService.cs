using System;
using System.Collections.Generic;
using System.Text.Json;
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
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CAServer.ThirdPart.Alchemy;

public class AlchemyOrderAppService : CAServerAppService, IAlchemyOrderAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<AlchemyOrderAppService> _logger;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IAlchemyServiceAppService _alchemyServiceAppService;
    private readonly IObjectMapper _objectMapper;
    private readonly AlchemyOptions _alchemyOptions;
    private readonly IAlchemyProvider _alchemyProvider;
    private readonly AlchemyHelper _alchemyHelper;

    public AlchemyOrderAppService(IClusterClient clusterClient,
        IThirdPartOrderProvider thirdPartOrderProvider,
        IDistributedEventBus distributedEventBus,
        IAlchemyServiceAppService alchemyServiceAppService,
        ILogger<AlchemyOrderAppService> logger,
        IOptions<ThirdPartOptions> merchantOptions,
        IAlchemyProvider alchemyProvider,
        IObjectMapper objectMapper,
        AlchemyHelper alchemyHelper)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _distributedEventBus = distributedEventBus;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _logger = logger;
        _alchemyOptions = merchantOptions.Value.alchemy;
        _alchemyServiceAppService = alchemyServiceAppService;
        _alchemyProvider = alchemyProvider;
        _alchemyHelper = alchemyHelper;
    }


    public Task<T> VerifyAlchemySignature<T>(Dictionary<string, string> inputDict)
    {
        const string signatureKey = "signature";

        // calculate a new wanted sign
        var expectedSignature =
            _alchemyHelper.GetAlchemySignatureAsync(inputDict, _alchemyOptions.AppSecret,
                new List<string>() { signatureKey });

        // compare two sign
        if (expectedSignature.Signature != inputDict.GetOrDefault(signatureKey))
        {
            _logger.LogWarning(
                "ACH signature verification failed, order id :{OrderNo}, and the signature :{Signature}",
                inputDict.GetOrDefault("merchantOrderNo"), inputDict.GetOrDefault(signatureKey));
            throw new UserFriendlyException(
                $"ACH signature verification failed, order id :{inputDict.GetOrDefault("merchantOrderNo")}, and the signature is:{inputDict.GetOrDefault(signatureKey)}");
        }

        return Task.FromResult(JsonSerializer.Deserialize<T>(
            JsonSerializer.Serialize(inputDict), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            }));
    }

    public async Task<BasicOrderResult> UpdateAlchemyOrderAsync(AlchemyOrderUpdateDto input)
    {
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
}