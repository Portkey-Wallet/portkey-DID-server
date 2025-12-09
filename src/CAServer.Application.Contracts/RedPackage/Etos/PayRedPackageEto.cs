using System;
using CAServer.EnumType;
using Volo.Abp.EventBus;

namespace CAServer.RedPackage.Etos;
[EventName("PayRedPackageEto")]

public class PayRedPackageEto
{
    public Guid RedPackageId { get; set; }
    
    public RedPackageDisplayType DisplayType { get; set; }
    
    public Guid ReceiverId { get; set; }
}