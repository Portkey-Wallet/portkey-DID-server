using System.Collections.Generic;

namespace CAServer.Growth.Dtos;

public class IndexerReferralInfo
{
    public string CaHash { get; set; }
    public string MethodName { get; set; }
    public string ReferralCode { get; set; }
    public string ProjectCode { get; set; }
    
    public long Timestamp { get; set; }
}

public class ReferralInfoDto
{
    public List<IndexerReferralInfo> ReferralInfo { get; set; }
}