using System.Collections.Generic;

namespace CAServer.Growth.Dtos;

public class ReferralRequestDto
{
    public List<string> CaHashes { get; set; }
    public ReferralType ReferralType { get; set; }
}

public enum ReferralType
{
    CreateCAHolder,
    SocialRecovery
}