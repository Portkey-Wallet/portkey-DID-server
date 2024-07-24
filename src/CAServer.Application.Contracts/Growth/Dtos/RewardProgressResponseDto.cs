using System.Collections.Generic;

namespace CAServer.Growth.Dtos;

public class RewardProgressResponseDto
{

    public List<ReferralCountDto> Data { get; set; }

    public string RewardProcessCount { get; set; }

}

public class ReferralCountDto
{
    public string ActivityName { get; set; }
    
    public string ReferralCount{get; set; }
}


