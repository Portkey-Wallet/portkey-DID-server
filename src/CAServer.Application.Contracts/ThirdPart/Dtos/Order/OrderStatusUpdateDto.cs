using System.Collections.Generic;

namespace CAServer.ThirdPart.Dtos;

public class OrderStatusUpdateDto
{
    public string OrderId { get; set; }
    public OrderDto Order { get; set; }
    public OrderStatusType Status { get; set; }
    public string RawTransaction { get; set; }
    public Dictionary<string, object> DicExt { get; set; }
}