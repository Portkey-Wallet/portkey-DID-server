namespace CAServer.Grains.State.Growth;

[GenerateSerializer]
public class GrowthState
{
	[Id(0)]
    public string Id { get; set; }
	[Id(1)]
    public Guid UserId { get; set; }
	[Id(2)]
    public string CaHash { get; set; }
	[Id(3)]
    public string InviteCode { get; set; }
	[Id(4)]
    public string ReferralCode { get; set; }
	[Id(5)]
    public string ProjectCode { get; set; }
	[Id(6)]
    public string ShortLinkCode { get; set; }
	[Id(7)]
    public DateTime CreateTime { get; set; }
	[Id(8)]
    public bool IsDeleted { get; set; }

	[Id(9)]
    public List<InviteInfo> InviteInfos { get; set; } = new();
}

[GenerateSerializer]
public class InviteInfo
{
	[Id(0)]
    public string Id { get; set; }
	[Id(1)]
    public string InviteCode { get; set; }
	[Id(2)]
    public string ReferralCode { get; set; }
	[Id(3)]
    public string ProjectCode { get; set; }
	[Id(4)]
    public string ShortLinkCode { get; set; }
	[Id(5)]
    public DateTime CreateTime { get; set; }
	[Id(6)]
    public bool IsDeleted { get; set; }
}
