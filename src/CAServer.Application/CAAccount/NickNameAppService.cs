﻿using System;
using System.Collections.Generic;
using CAServer.Dtos;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Grains.Grain.Contacts;
using Nest;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace CAServer.CAAccount;

[RemoteService(false)]
[DisableAuditing]
public class NickNameAppService : CAServerAppService, INickNameAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly INESTRepository<CAHolderIndex, Guid> _holderRepository;

    public NickNameAppService(IDistributedEventBus distributedEventBus, IClusterClient clusterClient,
        INESTRepository<CAHolderIndex, Guid> holderRepository)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _holderRepository = holderRepository;
    }

    public async Task<CAHolderResultDto> SetNicknameAsync(UpdateNickNameDto nickNameDto)
    {
        var userId = CurrentUser.GetId();
        var grain = _clusterClient.GetGrain<ICAHolderGrain>(userId);

        var result = await grain.UpdateNicknameAsync(nickNameDto.NickName);
        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<CAHolderGrainDto, UpdateCAHolderEto>(result.Data));
        return ObjectMapper.Map<CAHolderGrainDto, CAHolderResultDto>(result.Data);
    }

    public async Task<CAHolderResultDto> GetCaHolderAsync()
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CAHolderIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.UserId).Value(CurrentUser.GetId()))
        };
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.IsDeleted).Terms(false)));
        QueryContainer Filter(QueryContainerDescriptor<CAHolderIndex> f) => f.Bool(b => b.Must(mustQuery));

        var holder = await _holderRepository.GetAsync(Filter);
        return ObjectMapper.Map<CAHolderIndex, CAHolderResultDto>(holder);;
    }
    
    public async Task<CAHolderResultDto> DeleteAsync()
    {
        var userId = CurrentUser.GetId();
        var grain = _clusterClient.GetGrain<ICAHolderGrain>(userId);

        var result = await grain.DeleteAsync();
        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<CAHolderGrainDto, DeleteCAHolderEto>(result.Data));
        return ObjectMapper.Map<CAHolderGrainDto, CAHolderResultDto>(result.Data);
    }
}