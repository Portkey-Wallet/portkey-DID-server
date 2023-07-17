using CAServer.Grains.State.Order;
using CAServer.ThirdPart.Dtos;

namespace CAServer.Grains.Grain.ThirdPart;

public class OrderStatusInfoGrainResultDto
{
    public string Id { get; set; }
    public Guid OrderId { get; set; }
    public string ThirdPartOrderNo { get; set; }
    public string RawTransaction { get; set; }
    public List<OrderStatusInfo> OrderStatusList { get; set; } = new();
}