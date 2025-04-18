using CAServer.ThirdPart.Dtos;

namespace CAServer.Grains.State.Order;

[GenerateSerializer]
public class OrderStatusInfoState
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
    public List<OrderStatusInfo> OrderStatusList { get; set; } = new();
}
