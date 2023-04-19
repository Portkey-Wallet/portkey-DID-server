using System;
using CAServer.Chain;
using Volo.Abp.EventBus;

namespace CAServer.Etos.Chain;

[EventName("ChainCreateEto")]
public class ChainCreateEto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string ChainName { get; set; }
    public string EndPoint { get; set; }
    public string ExplorerUrl { get; set; }
    public string CaContractAddress { get; set; }
    public DateTime LastModifyTime { get; set; }
    public DefaultToken DefaultToken { get; set; }
}