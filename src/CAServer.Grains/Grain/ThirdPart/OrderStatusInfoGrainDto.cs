using CAServer.Grains.State.Order;
using CAServer.ThirdPart.Dtos;

namespace CAServer.Grains.Grain.ThirdPart;

public class OrderStatusInfoGrainDto
{
    public string Id { get; set; }
    public Guid OrderId { get; set; }
    public string ThirdPartOrderNo { get; set; }
    public string RawTransaction { get; set; }
    public OrderStatusInfo OrderStatusInfo { get; set; }
}