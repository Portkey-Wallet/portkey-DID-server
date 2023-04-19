using System;
using Volo.Abp.EventBus;

namespace CAServer.Notify.Etos;

[EventName("NotifyEto")]
public class NotifyEto : NotifyRulesBase
{
    public Guid Id { get; set; }
}