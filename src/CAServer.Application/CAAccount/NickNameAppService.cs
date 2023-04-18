using System;
using CAServer.Dtos;
using System.Threading.Tasks;
using CAServer.Etos;
using CAServer.Grains.Grain.Contacts;
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

    public NickNameAppService(IDistributedEventBus distributedEventBus, IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
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
}