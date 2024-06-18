using System;
using CAServer.EnumType;

namespace CAServer.Growth.Dtos;

public class ReferralRecordRankRequestDto
{
    public ActivityEnums Activity { get; set; }

    public ActivityCycleEnums ActivityCycle { get; set; }

    public string CaHash { get; set; }

}
