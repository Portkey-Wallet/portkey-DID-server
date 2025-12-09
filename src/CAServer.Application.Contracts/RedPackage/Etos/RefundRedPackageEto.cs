using System;
using Volo.Abp.EventBus;

namespace CAServer.RedPackage.Etos;

[EventName("RefundRedPackageEto")]
public class RefundRedPackageEto
{
    public Guid RedPackageId { get; set; }

}