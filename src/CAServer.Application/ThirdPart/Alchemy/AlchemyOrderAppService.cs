using System;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Logging;
using Orleans;
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


    public AlchemyOrderAppService(IClusterClient clusterClient,
        IThirdPartOrderProvider thirdPartOrderProvider,
        IDistributedEventBus distributedEventBus,
        ILogger<AlchemyOrderAppService> logger,
        IObjectMapper objectMapper)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _distributedEventBus = distributedEventBus;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _logger = logger;
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
        _logger.LogDebug($"This alchemy order {grainId} will be updated.");

        var result = await orderGrain.UpdateOrderAsync(dataToBeUpdated);

        if (!result.Success)
        {
            _logger.LogError($"Update user order fail, third part order number: {input.MerchantOrderNo}");
            return new BasicOrderResult() { Message = $"Update order failed,{result.Message}" };
        }

        await _distributedEventBus.PublishAsync(_objectMapper.Map<OrderGrainDto, OrderEto>(result.Data));
        return new BasicOrderResult() { Success = true, Message = result.Message };
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