using CAServer.Grains.State.UserGuide;
using CAServer.UserExtraInfo;
using Orleans;

namespace CAServer.Grains.Grain.UserGuide;

public interface IUserGuideGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<List<UserGuideInfoGrainDto>>> ListGrainResultDto();
    Task SetUserGuideInfoAsync(UserGuideGrainInput input);
    Task<GrainResultDto<bool>> FinishUserGuideInfoAsync(GuideType inputGuideType);
}