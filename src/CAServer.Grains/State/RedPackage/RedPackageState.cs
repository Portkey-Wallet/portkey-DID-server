using CAServer.EnumType;

namespace CAServer.Grains.State.RedPackage;
[Serializable]
[GenerateSerializer]
public class RedPackageState
{
	[Id(0)]
    public Guid SessionId { get; set; }
	[Id(1)]
    public RedPackageDisplayType RedPackageDisplayType { get; set; }
	[Id(2)]
    public bool IsNewUsersOnly { get; set; }
	[Id(3)]
    public Guid Id { get; set; }
	[Id(4)]
    public long TotalAmount { get; set; }
	[Id(5)]
    public long GrabbedAmount { get; set; }
	[Id(6)]
    public long MinAmount { get; set; }
	[Id(7)]
    public string Memo { get; set; } = string.Empty;
	[Id(8)]
    public string ChainId { get; set; }
	[Id(9)]
    public Guid SenderId { get; set; }
	[Id(10)]
    public Guid LuckKingId { get; set; } = Guid.Empty;
	[Id(11)]
    public long CreateTime { get; set; }
    //this will be set when the red package is not left
	[Id(12)]
    public long EndTime { get; set; }
	[Id(13)]
    public long ExpireTime { get; set; }
	[Id(14)]
    public string Symbol { get; set; }
	[Id(15)]
    public int Decimal { get; set; }
	[Id(16)]
    public int Count { get; set; }
	[Id(17)]
    public int Grabbed { get; set; }
	[Id(18)]
    public string ChannelUuid { get; set; }
	[Id(19)]
    public RedPackageType Type { get; set; }
	[Id(20)]
    public RedPackageStatus Status { get; set; }
	[Id(21)]
    public List<GrabItem> Items { get; set; }
	[Id(22)]
    public List<BucketItem> BucketNotClaimed { get; set; }
	[Id(23)]
    public List<BucketItem> BucketClaimed { get; set; }
	[Id(24)]
    public bool IfRefund{ get; set; }
    
	[Id(25)]
    public int AssetType { get; set; }
}

[GenerateSerializer]
public class GrabItem
{
	[Id(0)]
    public Guid UserId { get; set; }
	[Id(1)]
    public bool PaymentCompleted{ get; set; }
	[Id(2)]
    public string CaAddress { get; set; } = string.Empty;
	[Id(3)]
    public string Username { get; set; }
	[Id(4)]
    public string Avatar { get; set; }
	[Id(5)]
    public long GrabTime { get; set; }
	[Id(6)]
    public bool IsLuckyKing { get; set; }
	[Id(7)]
    public long Amount { get; set; }
	[Id(8)]
    public int Decimal { get; set; }
	[Id(9)]
    public string IpAddress { get; set; }
	[Id(10)]
    public string Identity { get; set; }
}

[GenerateSerializer]
public class BucketItem
{
	[Id(0)]
    public int Index { get; set; }
	[Id(1)]
    public long Amount { get; set; }
	[Id(2)]
    public bool IsLuckyKing { get; set; }
	[Id(3)]
    public Guid UserId { get; set; }
}
