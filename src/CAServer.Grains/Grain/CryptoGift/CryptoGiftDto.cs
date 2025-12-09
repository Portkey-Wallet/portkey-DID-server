using CAServer.Grains.State;

namespace CAServer.Grains.Grain.CryptoGift;

[GenerateSerializer]
public class CryptoGiftDto
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
    public List<PreGrabItem> Items { get; set; }

    [Id(7)]
    public List<PreGrabBucketItemDto> BucketNotClaimed { get; set; }

    [Id(8)]
    public List<PreGrabBucketItemDto> BucketClaimed { get; set; }
}