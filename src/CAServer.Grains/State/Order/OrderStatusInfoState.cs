using CAServer.ThirdPart.Dtos;

namespace CAServer.Grains.State.Order;

public class OrderStatusInfoState
{
    public string Id { get; set; }
    public Guid OrderId { get; set; }
    public string ThirdPartOrderNo { get; set; }
    public string RawTransaction { get; set; }
    public List<OrderStatusInfo> OrderStatusList { get; set; } = new();
}

public class OrderStatusInfo
{
    public OrderStatusType Status { get; set; }
    public long LastModifyTime { get; set; }
    public string Extension { get; set; }
}