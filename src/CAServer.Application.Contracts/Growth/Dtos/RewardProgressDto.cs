using CAServer.EnumType;

namespace CAServer.Growth.Dtos;

public class RewardProgressDto
{
    public string TargetClientId { get; set; }

    public ActivityEnums ActivityEnums { get; set; }
}