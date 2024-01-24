using System;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Grains.Grain.UserGuide;
using CAServer.Grains.State.UserGuide;
using CAServer.Options;
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
        var result = await GetAndAddUserGuideInfoAsync(null, currentUserId);
        return result;
    }

    public async Task<UserGuideDto> QueryUserGuideAsync(UserGuideRequestDto input, Guid? currentUserId)
    {
        var result = await GetAndAddUserGuideInfoAsync(input, currentUserId);
        return result;
    }

    public async Task<bool> FinishUserGuideAsync(UserGuideFinishRequestDto input, Guid? currentUserId)
    {
        if (null == currentUserId)
        {
            throw new UserFriendlyException("User not Login.");
        }

        var userGuideGrain = _clusterClient.GetGrain<IUserGuideGrain>(currentUserId.Value);
        var grainDto = await userGuideGrain.ListGrainResultDto();
        var userGuideInfoGrain = grainDto.Data;
        if (userGuideInfoGrain.Count == 0)
        {
            throw new UserFriendlyException("User guide info not found.");
        }

        var list = userGuideInfoGrain.Select(t => t.GuideType == input.GuideType).ToList();
        if (list.Count == 0)
        {
            throw new UserFriendlyException("User guide info not found.");
        }

        var resultDto = await userGuideGrain.FinishUserGuideInfoAsync(input.GuideType);
        return resultDto.Success;
    }

    private async Task<UserGuideDto> GetAndAddUserGuideInfoAsync(UserGuideRequestDto input, Guid? currentUserId)
    {
        if (null == currentUserId)
        {
            throw new UserFriendlyException("User not Login.");
        }

        var userGuideGrain = _clusterClient.GetGrain<IUserGuideGrain>(currentUserId.Value);
        var grainDto = await userGuideGrain.ListGrainResultDto();
        var guideDto = new UserGuideDto();
        var userGuideInfoGrain = grainDto.Data;
        var grainInput = new UserGuideGrainInput();
        if (userGuideInfoGrain.Count == 0)
        {
            var userGuideOptions = _userGuideInfoOptions.GuideInfos;
            foreach (var guideInfo in userGuideOptions.Select(userGuide =>
                         _objectMapper.Map<GuideInfo, UserGuideInfo>(userGuide)))
            {
                guideInfo.Status = 0;
                guideDto.UserGuideInfos.Add(guideInfo);
            }

            foreach (var guideDtoInfo in guideDto.UserGuideInfos)
            {
                grainInput.UserGuideInfoInputs.Add(
                    _objectMapper.Map<UserGuideInfo, UserGuideInfoGrainDto>(guideDtoInfo));
            }

            await userGuideGrain.SetUserGuideInfoAsync(grainInput);

            if (input == null)
            {
                return guideDto;
            }

            var result = guideDto.UserGuideInfos.Where(t => input.GuideTypes.Contains(Convert.ToInt32(t.GuideType)))
                .ToList();
            guideDto.UserGuideInfos = result;

            return guideDto;
        }

        foreach (var guideInfo in userGuideInfoGrain.Select(guideInfoGrain =>
                     _objectMapper.Map<UserGuideInfoGrainDto, UserGuideInfo>(guideInfoGrain)))
        {
            guideDto.UserGuideInfos.Add(guideInfo);
        }

        if (input == null)
        {
            return guideDto;
        }

        var list = guideDto.UserGuideInfos.Where(t => input.GuideTypes.Contains(Convert.ToInt32(t.GuideType)))
            .ToList();
        guideDto.UserGuideInfos = list;

        return guideDto;
    }
}