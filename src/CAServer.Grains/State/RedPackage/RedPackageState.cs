using CAServer.EnumType;

namespace CAServer.Grains.State.RedPackage;
[Serializable]
public class RedPackageState
{
    public Guid SessionId { get; set; }
    public RedPackageDisplayType RedPackageDisplayType { get; set; }
    public bool IsNewUsersOnly { get; set; }
    public Guid Id { get; set; }
    public long TotalAmount { get; set; }
    public long GrabbedAmount { get; set; }
    public long MinAmount { get; set; }
    public string Memo { get; set; } = string.Empty;
    public string ChainId { get; set; }
    public Guid SenderId { get; set; }
    public Guid LuckKingId { get; set; } = Guid.Empty;
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
    public bool IfRefund{ get; set; }
    
    public int AssetType { get; set; }
}

public class GrabItem
{
    public Guid UserId { get; set; }
    public bool PaymentCompleted{ get; set; }
    public string CaAddress { get; set; } = string.Empty;
    public string Username { get; set; }
    public string Avatar { get; set; }
    public long GrabTime { get; set; }
    public bool IsLuckyKing { get; set; }
    public long Amount { get; set; }
    public int Decimal { get; set; }
    public string IpAddress { get; set; }
    public string Identity { get; set; }
}

public class BucketItem
{
    public int Index { get; set; }
    public long Amount { get; set; }
    public bool IsLuckyKing { get; set; }
    public Guid UserId { get; set; }
}