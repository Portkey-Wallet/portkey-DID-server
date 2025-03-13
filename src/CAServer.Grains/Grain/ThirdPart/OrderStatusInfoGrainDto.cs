using CAServer.ThirdPart.Dtos;

namespace CAServer.Grains.Grain.ThirdPart;

[GenerateSerializer]
public class OrderStatusInfoGrainDto
{
    [Id(0)]
    public string Id { get; set; }
    [Id(1)]
    public Guid OrderId { get; set; }
    [Id(2)]
    public string ThirdPartOrderNo { get; set; }
    [Id(3)]
    public string RawTransaction { get; set; }
    [Id(4)]
    public OrderStatusInfo OrderStatusInfo { get; set; }
}