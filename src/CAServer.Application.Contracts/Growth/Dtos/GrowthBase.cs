using System;

namespace CAServer.Growth.Dtos;

public class GrowthBase
{
    public string Id { get; set; }
    public Guid UserId { get; set; }
    public string CaHash { get; set; }
    public string InviteCode { get; set; }
    public string ReferralCode { get; set; }
    public string ProjectCode { get; set; }
    public string ShortLinkCode { get; set; }
    public DateTime CreateTime { get; set; }
}