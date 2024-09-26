using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Account;
using CAServer.Entities.Es;
using CAServer.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core;

public class HolderStatisticHandler : IDistributedEventHandler<HolderExtraInfoEto>,
    IDistributedEventHandler<HolderExtraInfoCompletedEto>, ITransientDependency
{
    private readonly INESTRepository<HolderStatisticIndex, string> _holderStatisticRepository;
    private readonly ILogger<HolderStatisticHandler> _logger;

    public HolderStatisticHandler(INESTRepository<HolderStatisticIndex, string> holderStatisticRepository,
        ILogger<HolderStatisticHandler> logger)
    {
        _holderStatisticRepository = holderStatisticRepository;
        _logger = logger;
    }

    public async Task HandleEventAsync(HolderExtraInfoEto eventData)
    {
        try
        {
            if (eventData.ExtraInfo.IsNullOrEmpty()) return;

            var statisticIndex = new HolderStatisticIndex
            {
                Id = eventData.GrainId,
                OperationType = eventData.OperationType.ToString(),
                CreateTime = DateTime.UtcNow,
                Status = AccountOperationStatus.Pending.ToString()
            };

            if (eventData.ExtraInfo.TryGetValue("activityId", out var activityId))
            {
                statisticIndex.ActivityId = activityId.ToString();
            }

            if (eventData.ExtraInfo.TryGetValue("ip", out var ip))
            {
                // get country from ip
                statisticIndex.CountryName = "";
            }

            await _holderStatisticRepository.AddOrUpdateAsync(statisticIndex);
            _logger.LogInformation(
                "save HolderExtraInfo success, grainId:{grainId},ip:{ip},country:{country},activityId:{activityId}",
                statisticIndex.Id, statisticIndex.IpAddress ?? "-", statisticIndex.CountryName ?? "-",
                statisticIndex.ActivityId ?? "-");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "save HolderExtraInfo error, data:{data}", JsonConvert.SerializeObject(eventData));
        }
    }

    public async Task HandleEventAsync(HolderExtraInfoCompletedEto eventData)
    {
        try
        {
            var statisticIndex = await _holderStatisticRepository.GetAsync(eventData.GrainId);
            if (statisticIndex == null) return;
            
            statisticIndex.CaAddress = eventData.CaAddress;
            statisticIndex.CaHash = eventData.CaHash;
            statisticIndex.Status = eventData.Status;
            
            await _holderStatisticRepository.AddOrUpdateAsync(statisticIndex);
            _logger.LogInformation(
                "save completed HolderExtraInfo success, grainId:{grainId},ip:{ip},country:{country},activityId:{activityId}",
                statisticIndex.Id, statisticIndex.IpAddress ?? "-", statisticIndex.CountryName ?? "-",
                statisticIndex.ActivityId ?? "-");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "save completed HolderExtraInfo error, data:{data}", JsonConvert.SerializeObject(eventData));
        }
    }
}