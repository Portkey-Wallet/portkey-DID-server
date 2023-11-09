using System;
using System.Collections.Generic;

namespace CAServer.ThirdPart.Dtos.Order;

public class OrderStatusSection : BaseOrderSection
{
    
    public OrderStatusSection() : base(OrderSectionEnum.OrderStateSection)
    {
    }
    
    public string Id { get; set; }
    public Guid OrderId { get; set; }
    public string ThirdPartOrderNo { get; set; }
    public List<OrderStatusInfo> OrderStatusList { get; set; }

}