using System;
using Volo.Abp.EventBus;

namespace CAServer.Notify.Etos;

[EventName("DeleteNotifyEto")]
public class DeleteNotifyEto : NotifyRulesBase
{
    public Guid Id { get; set; }
}