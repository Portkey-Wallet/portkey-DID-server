using CAServer.Grains.State.UserGuide;

namespace CAServer.Grains.Grain.UserGuide;

public class UserGuideGrain : Orleans.Grain<UserGuideState>, IUserGuideGrain
{
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<GrainResultDto<List<UserGuideInfoGrainDto>>> ListGrainResultDto()
    {
        var userGuideInfoGrainDtos = State.UserGuideInfos;
        if (userGuideInfoGrainDtos.Count == 0)
        {
            return new GrainResultDto<List<UserGuideInfoGrainDto>>(new List<UserGuideInfoGrainDto>());
        }

        return new GrainResultDto<List<UserGuideInfoGrainDto>>()
        {
            Data = userGuideInfoGrainDtos,
            Success = true
        };
    }

    public async Task SetUserGuideInfoAsync(UserGuideGrainInput input)
    {
        State.UserGuideInfos = input.UserGuideInfoInputs;
        await WriteStateAsync();
    }
}