using Orleans;

namespace CAServer.ThirdPart.Dtos;

[GenerateSerializer]
public class OrderStatusInfo
{
    [Id(0)]
    public string Status { get; set; }
    [Id(1)]
    public long LastModifyTime { get; set; }
    [Id(2)]
    public string Extension { get; set; }
}