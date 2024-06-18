using System.Collections.Generic;

namespace CAServer.Growth.Dtos;

public class ReferralRecordsRankResponseDto
{
    public List<ReferralRecordsRankDetail> ReferralRecordsRank { get; set; }

    public ReferralRecordsRankDetail CurrentUserReferralRecordsRankDetail { get; set; }
}

public class ReferralRecordsRankDetail
{
    public string CaAddress { get; set; }

    public int ReferralTotalCount { get; set; }

    public int Rank { get; set; }

    public string Avatar { get; set; }

}