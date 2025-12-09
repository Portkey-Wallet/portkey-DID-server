using System;
using CAServer.Chain;
using Volo.Abp.EventBus;

namespace CAServer.Etos.Chain;

[EventName("ChainDeleteEto")]
public class ChainDeleteEto : ChainDto
{
    public string Id { get; set; }
}