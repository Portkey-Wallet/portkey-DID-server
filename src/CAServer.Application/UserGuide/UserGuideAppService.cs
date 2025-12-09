using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Grains.Grain.UserGuide;
using CAServer.Options;
using CAServer.UserExtraInfo;
using CAServer.UserGuide.Dtos;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace CAServer.UserGuide;

public class UserGuideAppService : IUserGuideAppService, ITransientDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly UserGuideInfoOptions _userGuideInfoOptions;
    private readonly IObjectMapper _objectMapper;


    public UserGuideAppService(IClusterClient clusterClient,
        IOptionsSnapshot<UserGuideInfoOptions> userGuideInfoOptions,
        IObjectMapper objectMapper)
    {
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _userGuideInfoOptions = userGuideInfoOptions.Value;
    }


    public async Task<UserGuideDto> ListUserGuideAsync(Guid? currentUserId)
    {
        var guideInfos = await GetUserGuideInfoAsync(null, currentUserId);
        return guideInfos;
    }

    public async Task<UserGuideDto> QueryUserGuideAsync(UserGuideRequestDto input, Guid? currentUserId)
    {
        var guideInfos = await GetUserGuideInfoAsync(input, currentUserId);
        if (guideInfos == null || guideInfos.UserGuideInfos.IsNullOrEmpty())
        {
            return guideInfos;
        }
        foreach (var guideInfosUserGuideInfo in guideInfos.UserGuideInfos)
        {
            if (guideInfosUserGuideInfo == null)
            {
                continue;
            }
            if (GuideType.AiChat.Equals(guideInfosUserGuideInfo.GuideType)
                || GuideType.FinishAiChat.Equals(guideInfosUserGuideInfo.GuideType))
            {
                guideInfosUserGuideInfo.Status = 1;
            }
        }
        return guideInfos;
    }

    public async Task<bool> FinishUserGuideAsync(UserGuideFinishRequestDto input, Guid? currentUserId)
    {
        if (null == currentUserId)
        {
            throw new UserFriendlyException("User not Login.");
        }

        var userGuideGrain = _clusterClient.GetGrain<IUserGuideGrain>(currentUserId.Value);
        var resultDto = await userGuideGrain.FinishUserGuideInfoAsync(input.GuideType);
        return resultDto.Success;
    }

    private async Task<UserGuideDto> GetUserGuideInfoAsync(UserGuideRequestDto input, Guid? currentUserId)
    {
        if (null == currentUserId)
        {
            throw new UserFriendlyException("User not Login.");
        }

        var userGuideGrain = _clusterClient.GetGrain<IUserGuideGrain>(currentUserId.Value);
        var grainDto = await userGuideGrain.ListGrainResultDto();
        var guideDto = new UserGuideDto();
        var userGuideInfoGrain = grainDto.Data;

        var userGuideOptions = _userGuideInfoOptions.GuideInfos;
        if (userGuideInfoGrain.Count == 0)
        {
            foreach (var guideInfo in userGuideOptions.Select(userGuide =>
                         _objectMapper.Map<GuideInfo, UserGuideInfo>(userGuide)))
            {
                guideInfo.Status = 0;
                guideDto.UserGuideInfos.Add(guideInfo);
            }

            if (input != null)
            {
                guideDto.UserGuideInfos = guideDto.UserGuideInfos
                    .Where(t => input.GuideTypes.Contains(Convert.ToInt32(t.GuideType))).ToList();
            }

            return guideDto;
        }

        foreach (var guideInfo in userGuideOptions)
        {
            var info = _objectMapper.Map<GuideInfo, UserGuideInfo>(guideInfo);
            var userGuideInfo =
                userGuideInfoGrain.FirstOrDefault(t => Convert.ToInt32(t.GuideType) == guideInfo.GuideType);
            if (userGuideInfo == null)
            {
                info.Status = 0;
                guideDto.UserGuideInfos.Add(info);
                continue;
            }

            info.Status = userGuideInfo.Status;
            guideDto.UserGuideInfos.Add(info);
        }

        if (input != null)
        {
            guideDto.UserGuideInfos = guideDto.UserGuideInfos
                .Where(t => input.GuideTypes.Contains(Convert.ToInt32(t.GuideType))).ToList();
        }

        return guideDto;
    }
}