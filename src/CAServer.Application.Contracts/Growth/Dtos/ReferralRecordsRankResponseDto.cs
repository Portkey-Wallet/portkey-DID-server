using System.Collections.Generic;

namespace CAServer.Growth.Dtos;

public class ReferralRecordsRankResponseDto
{
    public List<ReferralRecordsRankDetail> ReferralRecordsRank { get; set; }

    public ReferralRecordsRankDetail CurrentUserReferralRecordsRankDetail { get; set; }

    public bool HasNext { get; set; }

    public string Invitations { get; set; }
}

public class ReferralRecordsRankDetail
{
    public string CaAddress { get; set; }

    public int ReferralTotalCount { get; set; }

    public int Rank { get; set; }

    public string Avatar { get; set; }

    public string WalletName { get; set; }

}