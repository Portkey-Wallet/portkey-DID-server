namespace CAServer.Grains.State.Growth;

public class GrowthState
{
    public string Id { get; set; }
    public Guid UserId { get; set; }
    public string CaHash { get; set; }
    public string InviteCode { get; set; }
    public string ReferralCode { get; set; }
    public string ProjectCode { get; set; }
    public string ShortLinkCode { get; set; }
    public DateTime CreateTime { get; set; }
    public bool IsDeleted { get; set; }

    public List<InviteInfo> InviteInfos { get; set; } = new();
}

public class InviteInfo
{
    public string Id { get; set; }
    public string InviteCode { get; set; }
    public string ReferralCode { get; set; }
    public string ProjectCode { get; set; }
    public string ShortLinkCode { get; set; }
    public DateTime CreateTime { get; set; }
    public bool IsDeleted { get; set; }
}