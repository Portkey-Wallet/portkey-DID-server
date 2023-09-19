using System;
using System.Collections.Generic;
using CAServer.Dtos;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Contacts;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Grains.Grain.Contacts;
using CAServer.Options;
using DnsClient.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;
using Environment = System.Environment;

namespace CAServer.CAAccount;

[RemoteService(false)]
[DisableAuditing]
public class NickNameAppService : CAServerAppService, INickNameAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly INESTRepository<CAHolderIndex, Guid> _holderRepository;
    private readonly IImRequestProvider _imRequestProvider;
    private readonly HostInfoOptions _hostInfoOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NickNameAppService(IDistributedEventBus distributedEventBus, IClusterClient clusterClient,
        INESTRepository<CAHolderIndex, Guid> holderRepository, IImRequestProvider imRequestProvider,
        IOptionsSnapshot<HostInfoOptions> hostInfoOptions, IHttpContextAccessor httpContextAccessor)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _holderRepository = holderRepository;
        _imRequestProvider = imRequestProvider;
        _hostInfoOptions = hostInfoOptions.Value;
        _httpContextAccessor = httpContextAccessor;
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

        await UpdateImUserAsync(userId, nickNameDto.NickName);
        
        return ObjectMapper.Map<CAHolderGrainDto, CAHolderResultDto>(result.Data);
    }

    private async Task UpdateImUserAsync(Guid userId, string nickName)
    {
        if (_hostInfoOptions.Environment == Options.Environment.Development)
        {
            return;
        }

        var imUserUpdateDto = new ImUserUpdateDto
        {
            Name = nickName
        };

        try
        {
            await _imRequestProvider.PostAsync<object>(ImConstant.UpdateImUserUrl, imUserUpdateDto);
            Logger.LogInformation("{userId} update im user : {name}", userId.ToString(), nickName);
        }
        catch (Exception e)
        {
            Logger.LogError(e, ImConstant.ImServerErrorPrefix + " update im user fail : {userId}, {name}, {imToken}", 
                userId.ToString(), nickName, 
                _httpContextAccessor?.HttpContext?.Request?.Headers[CommonConstant.ImAuthHeader]);
        }
        
    }

    public async Task<CAHolderResultDto> GetCaHolderAsync()
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CAHolderIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.UserId).Value(CurrentUser.GetId()))
        };
        //mustQuery.Add(q => q.Terms(i => i.Field(f => f.IsDeleted).Terms(false)));
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