using System.Collections.Generic;

namespace CAServer.Growth.Dtos;

public class ReferralResponseDto
{
    public List<Referral> ReferralInfos { get; set; } = new();
}

public class Referral
{
    public string CaHash { get; set; }
    public string ReferralCode { get; set; }
    public string InviteCode { get; set; }
    public string ProjectCode { get; set; }
    public List<Referral> Children { get; set; } = new();
}