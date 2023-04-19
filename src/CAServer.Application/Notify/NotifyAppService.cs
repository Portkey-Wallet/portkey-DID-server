using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.Notify;
using CAServer.Notify.Dtos;
using CAServer.Notify.Etos;
using Nest;
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

    public NotifyAppService(IDistributedEventBus distributedEventBus,
        IClusterClient clusterClient,
        INESTRepository<NotifyRulesIndex, Guid> notifyRulesRepository)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _notifyRulesRepository = notifyRulesRepository;
    }

    public async Task<PullNotifyResultDto> PullNotifyAsync(PullNotifyDto input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NotifyRulesIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.DeviceTypes).Terms(input.DeviceType.ToString())));
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.AppVersions).Terms(input.AppVersion)));

        QueryContainer Filter(QueryContainerDescriptor<NotifyRulesIndex> f) => f.Bool(b => b.Must(mustQuery));
        IPromise<IList<ISort>> Sort(SortDescriptor<NotifyRulesIndex> s) => s.Descending(a => a.AppId);

        var (totalCount, notifyRulesIndices) = await _notifyRulesRepository.GetSortListAsync(Filter, sortFunc: Sort);

        if (totalCount <= 0)
        {
            return null;
        }

        var notifyRules = notifyRulesIndices.First();
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

        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<NotifyGrainDto, DeleteNotifyEto>(resultDto.Data));
    }
}