using CAServer.RedPackage.Dtos;

namespace CAServer.Grains.State.RedPackage;

public class RedPackageState
{
    public Guid Id { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal GrabbedAmount { get; set; }
    public decimal MinAmount { get; set; }
    public string Memo { get; set; } = string.Empty;
    public Guid SenderId { get; set; }
    public long CreateTime { get; set; }
    public long EndTime { get; set; }
    public long ExpireTime { get; set; }
    public string Symbol { get; set; }
    public int Decimal { get; set; }
    public int Count { get; set; }
    public int Grabbed { get; set; }
    public string ChannelUuid { get; set; }
    public RedPackageType Type { get; set; };
    public RedPackageStatus Status { get; set; } = RedPackageStatus.Init;
    public List<GrabItem> Items { get; set; }
    public int LuckyKingIndex { get; set; }
    public List<decimal> Bucket { get; set; }
}

public class GrabItem
{
    public Guid UserId { get; set; }
    public long GrabTime { get; set; }
    public bool IsLuckyKing { get; set; }
    public decimal Amount { get; set; }
}