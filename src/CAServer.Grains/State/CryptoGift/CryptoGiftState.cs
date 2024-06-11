using CAServer.EnumType;

namespace CAServer.Grains.State;

public class CryptoGiftState
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public long TotalAmount { get; set; }
    public long PreGrabbedAmount { get; set; }
    public long CreateTime { get; set; }
    public string Symbol { get; set; }
    
    public bool IsNewUsersOnly { get; set; }
    public List<PreGrabItem> Items { get; set; }
    
    public List<PreGrabBucketItemDto> BucketNotClaimed { get; set; }
    
    public List<PreGrabBucketItemDto> BucketClaimed { get; set; }
}

public class PreGrabBucketItemDto
{
    public int Index { get; set; }
    public long Amount { get; set; }
    public Guid UserId { get; set; }
    public string IdentityCode { get; set; }
}

public class PreGrabItem
{
    public int Index { get; set; }
    public GrabbedStatus GrabbedStatus { get; set; }
    public string IpAddress { get; set; } 
    public string IdentityCode { get; set; }
    public long GrabTime { get; set; }
    public long Amount { get; set; }
    public int Decimal { get; set; }
}