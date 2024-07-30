using System.Collections.Generic;

namespace CAServer.Options;

public class HamsterOptions
{
    public int MinAcornsScore { get; set; }

    public double HamsterReward { get; set; }

    public string Invitations { get; set; }

    public string HamsterEndPoints { get; set; }

    public string AddressPrefix { get; set; }

    public string AddressSuffix { get; set; }

    public Dictionary<string, string> HamsterCopyWriting { get; set; }

    public int HamsterExpired { get; set; }
    
    public double ReferralReward { get; set; }
}