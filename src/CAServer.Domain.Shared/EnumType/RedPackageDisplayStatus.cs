namespace CAServer.EnumType;

public static class RedPackageDisplayStatus
{
    private const string Active = "Active";
    private const string FullyClaimed = "All claimed";
    private const string Expired = "Expired";
    private const string Cancelled = "Cancelled";

    public static string GetDisplayStatus(RedPackageStatus redPackageStatus)
    {
        return redPackageStatus switch
        {
            RedPackageStatus.Init => Active,
            RedPackageStatus.Claimed => Active,
            RedPackageStatus.NotClaimed => Active,
            RedPackageStatus.FullyClaimed => FullyClaimed,
            RedPackageStatus.Expired => Expired,
            RedPackageStatus.Cancelled => Cancelled,
            _ => "UnKnown"
        };
    }
}