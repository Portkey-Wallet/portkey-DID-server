using System;
using System.Collections.Generic;
using CAServer.ThirdPart.Dtos;
using Volo.Abp.EventBus;

namespace CAServer.ThirdPart.Etos;

[EventName("OrderStatusInfoEto")]
public class OrderStatusInfoEto
{
    public string Id { get; set; }
    public Guid OrderId { get; set; }
    public string ThirdPartOrderNo { get; set; }
    public List<OrderStatusInfo> OrderStatusList { get; set; }
}