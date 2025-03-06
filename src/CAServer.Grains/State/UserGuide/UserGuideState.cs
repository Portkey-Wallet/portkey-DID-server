using CAServer.UserExtraInfo;

namespace CAServer.Grains.State.UserGuide;

[GenerateSerializer]
public class UserGuideState
{
	[Id(0)]
    public List<UserGuideInfoGrainDto> UserGuideInfos { get; set; } = new();
}

[GenerateSerializer]
public class UserGuideInfoGrainDto
{
	[Id(0)]
    public int Status { get; set; }

	[Id(1)]
    public GuideType GuideType { get; set; }

}
