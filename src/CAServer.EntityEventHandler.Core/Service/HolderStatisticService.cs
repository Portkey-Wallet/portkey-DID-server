using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Account;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.IpInfo;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core.Service;

public interface IHolderStatisticService
{
    Task HandleHolderExtraInfoAsync(HolderExtraInfoEto eventData);
    Task HandleHolderExtraInfoCompletedAsync(HolderExtraInfoCompletedEto eventData);
}

public class HolderStatisticService : IHolderStatisticService, ISingletonDependency
{
    private readonly INESTRepository<HolderStatisticIndex, string> _holderStatisticRepository;
    private readonly ILogger<HolderStatisticService> _logger;
    private readonly IIpInfoClient _infoClient;
    private readonly IObjectMapper _objectMapper;

    private const string IpPattern =
        @"^([0,1]?\d{1,2}|2([0-4][0-9]|5[0-5]))(\.([0,1]?\d{1,2}|2([0-4][0-9]|5[0-5]))){3}$";

    private const string IpKey = "ip";
    private const string ActivityIdKey = "activityId";
    private const string IpAddressKey = "ipAddress";

    public HolderStatisticService(INESTRepository<HolderStatisticIndex, string> holderStatisticRepository,
        ILogger<HolderStatisticService> logger, IIpInfoClient infoClient, IObjectMapper objectMapper)
    {
        _holderStatisticRepository = holderStatisticRepository;
        _logger = logger;
        _infoClient = infoClient;
        _objectMapper = objectMapper;
    }

    public async Task HandleHolderExtraInfoAsync(HolderExtraInfoEto eventData)
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

            if (eventData.ExtraInfo.TryGetValue(ActivityIdKey, out var activityId))
            {
                statisticIndex.ActivityId = activityId.ToString();
            }

            if (eventData.ExtraInfo.TryGetValue(IpAddressKey, out var ipAddress))
            {
                statisticIndex.IpAddress = ipAddress.ToString();
            }

            if (eventData.ExtraInfo.TryGetValue(IpKey, out var ip))
            {
                statisticIndex.IpAddress = ip.ToString();
            }

            if (!statisticIndex.IpAddress.IsNullOrEmpty())
            {
                statisticIndex.CountryInfo = await GetCountryInfoAsync(statisticIndex.IpAddress);
            }

            await _holderStatisticRepository.AddOrUpdateAsync(statisticIndex);
            _logger.LogInformation(
                "save HolderExtraInfo success, grainId:{grainId},ip:{ip},country:{country},activityId:{activityId}",
                statisticIndex.Id, statisticIndex.IpAddress ?? "-", statisticIndex.CountryInfo?.CountryName ?? "-",
                statisticIndex.ActivityId ?? "-");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[HandleMessageError] type:{type}, data:{data}, errMsg:{errMsg}",
                eventData.GetType().Name, JsonConvert.SerializeObject(eventData), e.StackTrace ?? "-");
        }
    }

    public async Task HandleHolderExtraInfoCompletedAsync(HolderExtraInfoCompletedEto eventData)
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
                statisticIndex.Id, statisticIndex.IpAddress ?? "-", statisticIndex.CountryInfo?.CountryName ?? "-",
                statisticIndex.ActivityId ?? "-");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[HandleMessageError] type:{type}, data:{data}, errMsg:{errMsg}",
                eventData.GetType().Name, JsonConvert.SerializeObject(eventData), e.StackTrace ?? "-");
        }
    }

    private async Task<CountryInfo> GetCountryInfoAsync(string ip)
    {
        if (!(new Regex(IpPattern).IsMatch(ip)))
        {
            return null;
        }

        var countryInfo = await _infoClient.GetCountryInfoAsync(ip);
        if (countryInfo == null)
        {
            return null;
        }

        if (countryInfo.Error != null)
        {
            _logger.LogError("get ip info error, ip:{0}, error info:{1}", ip,
                JsonConvert.SerializeObject(countryInfo.Error));
            return null;
        }

        return _objectMapper.Map<IpInfoDto, CountryInfo>(countryInfo);
    }
}