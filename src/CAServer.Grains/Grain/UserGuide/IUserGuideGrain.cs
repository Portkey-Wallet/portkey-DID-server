using CAServer.Grains.State.UserGuide;
using CAServer.UserExtraInfo;

namespace CAServer.Grains.Grain.UserGuide;

public interface IUserGuideGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<List<UserGuideInfoGrainDto>>> ListGrainResultDto();
    Task<GrainResultDto<bool>> FinishUserGuideInfoAsync(GuideType inputGuideType);
}