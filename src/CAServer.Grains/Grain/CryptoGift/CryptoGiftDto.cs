using CAServer.Grains.State;

namespace CAServer.Grains.Grain.CryptoGift;

public class CryptoGiftDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public long TotalAmount { get; set; }
    public long PreGrabbedAmount { get; set; }
    public long CreateTime { get; set; }
    public string Symbol { get; set; }
    public List<PreGrabItem> Items { get; set; }
    
    public List<PreGrabBucketItemDto> BucketNotClaimed { get; set; }
    
    public List<PreGrabBucketItemDto> BucketClaimed { get; set; }
}