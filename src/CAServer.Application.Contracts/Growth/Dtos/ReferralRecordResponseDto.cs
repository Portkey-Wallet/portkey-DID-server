using System.Collections.Generic;

namespace CAServer.Growth.Dtos;

public class ReferralRecordResponseDto
{
    
    public bool HasNextPage { get; set; } = true;

    public List<ReferralRecordDetailDto> ReferralRecords { get; set; }
    
    

}

public class ReferralRecordDetailDto
{
    public string WalletName { get; set; }

    public bool IsDirectlyInvite { get; set; }

    public string ReferralDate { get; set; }

    public string Avatar { get; set; }

    public string RecordDesc { get; set; }





}