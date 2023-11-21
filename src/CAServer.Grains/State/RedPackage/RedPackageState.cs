using CAServer.RedPackage.Dtos;

namespace CAServer.Grains.State.RedPackage;

public class RedPackageState
{
    public Guid Id { get; set; }
    public long TotalAmount { get; set; }
    public long GrabbedAmount { get; set; }
    public long MinAmount { get; set; }
    public string Memo { get; set; } = string.Empty;
    public string ChainId { get; set; }
    public Guid SenderId { get; set; }
    public long CreateTime { get; set; }
    //this will be set when the red package is not left
    public long EndTime { get; set; }
    public long ExpireTime { get; set; }
    public string Symbol { get; set; }
    public int Decimal { get; set; }
    public int Count { get; set; }
    public int Grabbed { get; set; }
    public string ChannelUuid { get; set; }
    public RedPackageType Type { get; set; }
    public RedPackageStatus Status { get; set; }
    public List<GrabItem> Items { get; set; }
    public List<BucketItem> BucketNotClaimed { get; set; }
    public List<BucketItem> BucketClaimed { get; set; }
}

public class GrabItem
{
    public Guid UserId { get; set; }
    public bool PaymentCompleted{ get; set; }
    public string CaAddress { get; set; } = string.Empty;
    public long GrabTime { get; set; }
    public bool IsLuckyKing { get; set; }
    public long Amount { get; set; }
    public int Decimal { get; set; }

}

public class BucketItem
{
    public long Amount { get; set; }
    public bool IsLuckyKing { get; set; }
    public Guid UserId { get; set; }
}