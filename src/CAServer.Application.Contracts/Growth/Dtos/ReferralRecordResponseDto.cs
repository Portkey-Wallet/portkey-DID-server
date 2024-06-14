using System.Collections.Generic;

namespace CAServer.Growth.Dtos;

public class ReferralRecordResponseDto
{
    
    public int LastDayReferralTotalCount { get; set; }

    public List<ReferralRecordDetailDto> ReferralRecords { get; set; }

}

public class ReferralRecordDetailDto
{
    
    public string CaHash { get; set; }

    public string WalletName { get; set; }

    public bool IsDirectlyInvite { get; set; }

    public string ReferralDate { get; set; }

}