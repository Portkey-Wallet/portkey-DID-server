using CAServer.EnumType;

namespace CAServer.Grains.State;

[GenerateSerializer]
public class CryptoGiftState
{
	[Id(0)]
    public Guid Id { get; set; }
	[Id(1)]
    public Guid SenderId { get; set; }
	[Id(2)]
    public long TotalAmount { get; set; }
	[Id(3)]
    public long PreGrabbedAmount { get; set; }
	[Id(4)]
    public long CreateTime { get; set; }
	[Id(5)]
    public string Symbol { get; set; }
    
	[Id(6)]
    public bool IsNewUsersOnly { get; set; }
	[Id(7)]
    public List<PreGrabItem> Items { get; set; }
    
	[Id(8)]
    public List<PreGrabBucketItemDto> BucketNotClaimed { get; set; }
    
	[Id(9)]
    public List<PreGrabBucketItemDto> BucketClaimed { get; set; }
}

[GenerateSerializer]
public class PreGrabBucketItemDto
{
	[Id(0)]
    public int Index { get; set; }
	[Id(1)]
    public long Amount { get; set; }
	[Id(2)]
    public Guid UserId { get; set; }
	[Id(3)]
    public string IdentityCode { get; set; }
}

[GenerateSerializer]
public class PreGrabItem
{
	[Id(0)]
    public int Index { get; set; }
	[Id(1)]
    public GrabbedStatus GrabbedStatus { get; set; }
	[Id(2)]
    public string IpAddress { get; set; } 
	[Id(3)]
    public string IdentityCode { get; set; }
	[Id(4)]
    public long GrabTime { get; set; }
	[Id(5)]
    public long Amount { get; set; }
	[Id(6)]
    public int Decimal { get; set; }
}
