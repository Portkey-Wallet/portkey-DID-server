using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.Notify;
using CAServer.IpInfo;
using CAServer.Notify.Dtos;
using CAServer.Notify.Etos;
using CAServer.Notify.Provider;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.Notify;

[RemoteService(false), DisableAuditing]
public class NotifyAppService : CAServerAppService, INotifyAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly INESTRepository<NotifyRulesIndex, Guid> _notifyRulesRepository;
    private readonly INotifyProvider _notifyProvider;
    private readonly IIpInfoAppService _ipInfoAppService;

    public NotifyAppService(IDistributedEventBus distributedEventBus,
        IClusterClient clusterClient,
        INESTRepository<NotifyRulesIndex, Guid> notifyRulesRepository,
        INotifyProvider notifyProvider,
        IIpInfoAppService ipInfoAppService)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _notifyRulesRepository = notifyRulesRepository;
        _notifyProvider = notifyProvider;
        _ipInfoAppService = ipInfoAppService;
    }

    public async Task<PullNotifyResultDto> PullNotifyAsync(PullNotifyDto input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NotifyRulesIndex>, QueryContainer>>();
        //mustQuery.Add(q => q.Term(i => i.Field(f => f.AppId).Value(input.AppId)));
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.DeviceTypes).Terms(input.DeviceType.ToString())));
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.AppVersions).Terms(input.AppVersion)));
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.DeviceBrands).Terms(input.DeviceBrand)));
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.OperatingSystemVersions).Terms(input.OperatingSystemVersion)));

        QueryContainer Filter(QueryContainerDescriptor<NotifyRulesIndex> f) => f.Bool(b => b.Must(mustQuery));
        IPromise<IList<ISort>> Sort(SortDescriptor<NotifyRulesIndex> s) => s.Descending(t => t.NotifyId);

        var (totalCount, notifyRulesIndices) = await _notifyRulesRepository.GetSortListAsync(Filter, sortFunc: Sort);
        if (totalCount <= 0)
        {
            return null;
        }

        var notifyRules = notifyRulesIndices.First();
        if (notifyRules.Countries is { Length: > 0 })
        {
            var ipInfo = await _ipInfoAppService.GetIpInfoAsync();
            notifyRules = notifyRulesIndices?.Where(t => t.Countries.Contains(ipInfo.Code)).FirstOrDefault();
            if (notifyRules == null)
            {
                return null;
            }
        }

        var grain = _clusterClient.GetGrain<INotifyGrain>(notifyRules.Id);
        var resultDto = await grain.GetNotifyAsync();
        if (!resultDto.Success)
        {
            throw new UserFriendlyException(resultDto.Message);
        }

        return ObjectMapper.Map<NotifyGrainDto, PullNotifyResultDto>(resultDto.Data);
    }

    public async Task<NotifyResultDto> CreateAsync(CreateNotifyDto notifyDto)
    {
        var grainId = GuidGenerator.Create();
        var grain = _clusterClient.GetGrain<INotifyGrain>(grainId);

        var resultDto =
            await grain.AddNotifyAsync(
                ObjectMapper.Map<CreateNotifyDto, NotifyGrainDto>(notifyDto));

        if (!resultDto.Success)
        {
            throw new UserFriendlyException(resultDto.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<NotifyGrainDto, NotifyEto>(resultDto.Data));
        return ObjectMapper.Map<NotifyGrainDto, NotifyResultDto>(resultDto.Data);
    }

    public async Task<NotifyResultDto> UpdateAsync(Guid id, UpdateNotifyDto notifyDto)
    {
        var grain = _clusterClient.GetGrain<INotifyGrain>(id);

        var resultDto =
            await grain.UpdateNotifyAsync(
                ObjectMapper.Map<UpdateNotifyDto, NotifyGrainDto>(notifyDto));

        if (!resultDto.Success)
        {
            throw new UserFriendlyException(resultDto.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<NotifyGrainDto, NotifyEto>(resultDto.Data));
        return ObjectMapper.Map<NotifyGrainDto, NotifyResultDto>(resultDto.Data);
    }

    public async Task DeleteAsync(Guid id)
    {
        var grain = _clusterClient.GetGrain<INotifyGrain>(id);
        var resultDto = await grain.DeleteNotifyAsync(id);

        if (!resultDto.Success)
        {
            throw new UserFriendlyException(resultDto.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<NotifyGrainDto, DeleteNotifyEto>(resultDto.Data));
    }

    public async Task<List<NotifyResultDto>> CreateFromCmsAsync(string version)
    {
        //get data from cms
        var condition =
            "/items/upgradePush?fields=*,countries.country_id.value,deviceBrands.deviceBrand_id.value,deviceTypes.deviceType_id.value,targetVersion.value," +
            $"appVersions.appVersion_id.value,styleType.value,targetVersion.value&filter[targetVersion][value][_eq]={version}&filter[status][_eq]=published";
        
        Logger.LogDebug("before get data from cms.");
        var notifyDto = await GetDataAsync(condition);

        var result = new List<NotifyResultDto>();

        foreach (var notify in notifyDto.Data)
        {
            var grainId = GuidGenerator.Create();
            var grain = _clusterClient.GetGrain<INotifyGrain>(grainId);

            var resultDto = await grain.AddNotifyAsync(ObjectMapper.Map<CmsNotify, NotifyGrainDto>(notify));
            if (!resultDto.Success)
            {
                throw new UserFriendlyException(resultDto.Message);
            }

            await _distributedEventBus.PublishAsync(ObjectMapper.Map<NotifyGrainDto, NotifyEto>(resultDto.Data), false,
                false);
            result.Add(ObjectMapper.Map<NotifyGrainDto, NotifyResultDto>(resultDto.Data));
        }

        return result;
    }

    public async Task<NotifyResultDto> UpdateFromCmsAsync(Guid id)
    {
        var grain = _clusterClient.GetGrain<INotifyGrain>(id);
        var getResultDto = await grain.GetNotifyAsync();
        if (!getResultDto.Success)
        {
            throw new UserFriendlyException(getResultDto.Message);
        }

        var condition =
            "/items/upgradePush?fields=*,countries.country_id.value,deviceBrands.deviceBrand_id.value,deviceTypes.deviceType_id.value," +
            $"targetVersion.value,appVersions.appVersion_id.value,styleType.value,targetVersion.value&filter[id][_eq]={getResultDto.Data.NotifyId}";
        var notifyDto = await GetDataAsync(condition);

        var cmsNotify = notifyDto.Data.First();
        var resultDto = await grain.UpdateNotifyAsync(ObjectMapper.Map<CmsNotify, NotifyGrainDto>(cmsNotify));

        if (!resultDto.Success)
        {
            throw new UserFriendlyException(resultDto.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<NotifyGrainDto, NotifyEto>(resultDto.Data), false,
            false);
        return ObjectMapper.Map<NotifyGrainDto, NotifyResultDto>(resultDto.Data);
    }

    private async Task<CmsNotifyDto> GetDataAsync(string condition)
    {
        var notifyDto = await _notifyProvider.GetDataFromCms<CmsNotifyDto>(condition);

        if (notifyDto?.Data == null || notifyDto.Data.Count == 0)
        {
            throw new UserFriendlyException($"Get data from cms fail: {condition}");
        }

        Logger.LogDebug("Get data from cms: {data}", JsonConvert.SerializeObject(notifyDto));

        return notifyDto;
    }
}