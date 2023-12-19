using System;
using Volo.Abp.EventBus;

namespace CAServer.RedPackage.Etos;
[EventName("PayRedPackageEto")]

public class PayRedPackageEto
{
    public Guid RedPackageId { get; set; }
}