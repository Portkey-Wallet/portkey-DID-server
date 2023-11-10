using System;
using System.Collections.Generic;
using System.Linq;

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


    public long? StateTime(OrderStatusType status)
    {
        return OrderStatusList
            .Where(s => s.Status == status.ToString())
            .OrderBy(s => s.LastModifyTime)
            .Select(s => s.LastModifyTime)
            .FirstOrDefault();
    }
    
    
}