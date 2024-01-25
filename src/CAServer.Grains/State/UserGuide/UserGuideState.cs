using CAServer.UserExtraInfo;

namespace CAServer.Grains.State.UserGuide;

public class UserGuideState
{
    public List<UserGuideInfoGrainDto> UserGuideInfos { get; set; } = new();
}

public class UserGuideInfoGrainDto
{
    public int Status { get; set; }

    public GuideType GuideType { get; set; }

}
