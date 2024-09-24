using System;
using Volo.Abp.EventBus;

namespace CAServer.ZeroHoldings.Etos;


[EventName("ZeroHoldingsConfigEto")]
public class ZeroHoldingsConfigEto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; }
}