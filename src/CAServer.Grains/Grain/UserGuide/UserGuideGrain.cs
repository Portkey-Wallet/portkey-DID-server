using CAServer.Grains.State.UserGuide;
using CAServer.UserExtraInfo;

namespace CAServer.Grains.Grain.UserGuide;

public class UserGuideGrain : Orleans.Grain<UserGuideState>, IUserGuideGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, token);
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

    public async Task<GrainResultDto<bool>> FinishUserGuideInfoAsync(GuideType inputGuideType)
    {
        var guideInfoGrainDtos = State.UserGuideInfos;
        var grainDto = guideInfoGrainDtos.FirstOrDefault(t => t.GuideType == inputGuideType);
        if (grainDto != null)
        {
            grainDto.Status = 1;
        }
        else
        {
            grainDto = new UserGuideInfoGrainDto
            {
                GuideType = inputGuideType,
                Status = 1
            };
            State.UserGuideInfos.Add(grainDto);
        }

        await WriteStateAsync();
        return new GrainResultDto<bool>()
        {
            Data = true,
            Success = true
        };
    }
}