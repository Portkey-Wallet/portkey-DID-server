using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper.Internal.Mappers;
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

namespace CAServer.ThirdPart.Provider;

public interface ITreasuryOrderProvider : ITransientDependency
{
    Task DoSaveOrder(TreasuryOrderDto orderDto, Dictionary<string, string> externalData = null);
}


public class TreasuryOrderProvider : ITreasuryOrderProvider
{
    
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;

    public TreasuryOrderProvider(IClusterClient clusterClient, IDistributedEventBus distributedEventBus)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
    }


    public async Task DoSaveOrder(TreasuryOrderDto orderDto, Dictionary<string, string> externalData = null)
    {
        var orderStatusGrain = _clusterClient.GetGrain<IOrderStatusInfoGrain>(
            GrainIdHelper.GenerateGrainId(CommonConstant.TreasuryOrderStatusInfoPrefix,
                orderDto.RampOrderId.ToString()));
        await orderStatusGrain.AddOrderStatusInfo(new OrderStatusInfoGrainDto
        {
            OrderId = orderDto.Id,
            ThirdPartOrderNo = orderDto.ThirdPartOrderId,
            OrderStatusInfo = new OrderStatusInfo
            {
                Status = orderDto.Status,
                LastModifyTime = DateTime.UtcNow.ToUtcMilliSeconds(),
                Extension = externalData.IsNullOrEmpty() ? null : JsonConvert.SerializeObject(externalData),
            }
        });

        var treasuryOrderGrain = _clusterClient.GetGrain<ITreasuryOrderGrain>(orderDto.Id);
        await treasuryOrderGrain.SaveOrUpdateAsync(orderDto);
        await _distributedEventBus.PublishAsync(new TreasuryOrderEto(orderDto));
    }
}