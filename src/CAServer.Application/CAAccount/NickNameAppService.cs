﻿using System;
using System.Collections.Generic;
using System.Linq;
using CAServer.Dtos;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Contacts;
using CAServer.Contacts.Provider;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.Contacts;
using CAServer.Guardian;
using CAServer.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly HostInfoOptions _hostInfoOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly INicknameProvider _nicknameProvider;
    private readonly IGuardianAppService _guardianAppService;
    private readonly IUserProfilePictureProvider _userProfilePictureProvider;
    private readonly IContactProvider _contactProvider;

    public NickNameAppService(IDistributedEventBus distributedEventBus, IClusterClient clusterClient,
        INESTRepository<CAHolderIndex, Guid> holderRepository,
        IOptionsSnapshot<HostInfoOptions> hostInfoOptions, IHttpContextAccessor httpContextAccessor,
        INicknameProvider nicknameProvider, IGuardianAppService guardianAppService,
        IUserProfilePictureProvider userProfilePictureProvider,
        IContactProvider contactProvider)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _holderRepository = holderRepository;
        _hostInfoOptions = hostInfoOptions.Value;
        _httpContextAccessor = httpContextAccessor;
        _nicknameProvider = nicknameProvider;
        _guardianAppService = guardianAppService;
        _userProfilePictureProvider = userProfilePictureProvider;
        _contactProvider = contactProvider;
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

        //mustQuery.Add(q => q.Terms(i => i.Field(f => f.IsDeleted).Terms(false)));
        QueryContainer Filter(QueryContainerDescriptor<CAHolderIndex> f) => f.Bool(b => b.Must(mustQuery));

        var holder = await _holderRepository.GetAsync(Filter);
        return ObjectMapper.Map<CAHolderIndex, CAHolderResultDto>(holder);
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

    public async Task<CAHolderResultDto> UpdateHolderInfoAsync(HolderInfoDto holderInfo)
    {
        var userId = CurrentUser.GetId();
        var grain = _clusterClient.GetGrain<ICAHolderGrain>(userId);

        var result = await grain.UpdateHolderInfo(holderInfo);
        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<CAHolderGrainDto, UpdateCAHolderEto>(result.Data));
        return ObjectMapper.Map<CAHolderGrainDto, CAHolderResultDto>(result.Data);
    }

    public async Task<bool> GetPoppedUpAccountAsync()
    {
        var userId = CurrentUser.GetId();
        var grain = _clusterClient.GetGrain<ICAHolderGrain>(userId);
        GrainResultDto<CAHolderGrainDto> result = await grain.GetCaHolder();
        if (!result.Success || result.Data == null)
        {
            throw new UserFriendlyException(result.Message);
        }
        
        var caHolderGrainDto = result.Data;
        string nickname = caHolderGrainDto.UserId.ToString("N").Substring(0, 8);
        return !caHolderGrainDto.PopedUp && !caHolderGrainDto.ModifiedNickname &&
               caHolderGrainDto.Nickname.Equals(nickname);
    }

    public async Task<bool> GetBubblingAccountAsync()
    {
        var userId = CurrentUser.GetId();
        var grain = _clusterClient.GetGrain<ICAHolderGrain>(userId);
        GrainResultDto<CAHolderGrainDto> result = await grain.GetCaHolder();
        if (!result.Success || result.Data == null)
        {
            throw new UserFriendlyException(result.Message);
        }
        var caHolderGrainDto = result.Data;
        return caHolderGrainDto.PopedUp && !caHolderGrainDto.ModifiedNickname;
    }

    public async Task ReplaceUserNicknameAsync(ReplaceNicknameDto replaceNicknameDto)
    {
        var userId = CurrentUser.GetId();
        var grain = _clusterClient.GetGrain<ICAHolderGrain>(userId);
        GrainResultDto<CAHolderGrainDto> result = await grain.GetCaHolder();
        if (!result.Success || result.Data == null)
        {
            throw new UserFriendlyException(result.Message);
        }
        
        var caHolderGrainDto = result.Data;
        if (!replaceNicknameDto.ReplaceNickname)
        {
            await grain.UpdatePopUpAsync(true);
        }
        else
        {
            GuardianResultDto guardianResultDto = await _guardianAppService.GetGuardianIdentifiersAsync(new UpdateGuardianIdentifierDto()
            {
                UserId = userId,
                CaHash = replaceNicknameDto.CaHash,
                ChainId = replaceNicknameDto.ChainId
            });
            if (guardianResultDto == null || guardianResultDto.GuardianList == null || guardianResultDto.GuardianList.Guardians.IsNullOrEmpty())
            {
                throw new UserFriendlyException("can't find login guardian list, set login account failed");
            }
            await _nicknameProvider.ModifyNicknameHandler(guardianResultDto, userId, caHolderGrainDto);
        }
    }

    public DefaultAvatarResponse GetDefaultAvatars()
    {
        return new DefaultAvatarResponse()
        {
            DefaultAvatars = _userProfilePictureProvider.GetDefaultUserPictures()
        };
    }

    public async Task<List<CAHolderWithAddressResultDto>> QueryHolderInfosAsync(QueryUserInfosInput input)
    {
        var result = new List<CAHolderWithAddressResultDto>();
        if (input.AddressList.IsNullOrEmpty())
        {
            return result;
        }

        var guardiansDto = await _contactProvider.GetCaHolderInfoByAddressAsync(input.AddressList, "");
        var caHashList = guardiansDto.CaHolderInfo.Select(t => t.CaHash).Distinct().ToList();
        if (caHashList.Count == 0)
        {
            return result;
        }
        
        QueryContainer Filter(QueryContainerDescriptor<CAHolderIndex> q) =>
            q.Terms(i => i.Field(f => f.CaHash).Terms(caHashList));
        var holders = await _holderRepository.GetListAsync(Filter, limit: caHashList.Count, skip: 0);
        foreach (var caHolderIndex in holders.Item2)
        {
            var caHolderWithAddressResultDto = ObjectMapper.Map<CAHolderIndex, CAHolderWithAddressResultDto>(caHolderIndex);
            caHolderWithAddressResultDto.Address = guardiansDto.CaHolderInfo.First(t => t.CaHash == caHolderIndex.CaHash).CaAddress;
            result.Add(caHolderWithAddressResultDto);
        }

        return result;
    }
}