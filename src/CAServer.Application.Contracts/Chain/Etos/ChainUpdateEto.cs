using System;
using CAServer.Chain;
using Volo.Abp.EventBus;

namespace CAServer.Etos.Chain;

[EventName("ChainUpdateEto")]
public class ChainUpdateEto : ChainDto
{
    public string Id { get; set; }
}