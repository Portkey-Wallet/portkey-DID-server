using Orleans;

namespace CAServer.RedPackage.Dtos;

[GenerateSerializer]
public class GrabResultDto
{
    [Id(0)]
    public RedPackageGrabStatus Result { get; set; }
    [Id(1)]
    public string ErrorMessage { get; set; } = string.Empty;
    [Id(2)]
    public string Amount { get; set; }
    [Id(3)]
    public int Decimal { get; set; }
    [Id(4)]
    public RedPackageStatus Status { get; set; }
    [Id(5)]
    
    public long ExpireTime { get; set; }
    [Id(6)]
    public BucketItemDto BucketItem { get; set; }
}