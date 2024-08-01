using CAServer.EnumType;

namespace CAServer.Growth.Dtos;

public class ReferralRecordRequestDto
{
    public string CaHash { get; set; }

    public int Skip { get; set; }

    public int Limit { get; set; }

    public ActivityEnums ActivityEnums { get; set; }
    
    public string TargetClientId { get; set; }
}