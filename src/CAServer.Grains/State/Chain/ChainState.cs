using CAServer.Chain;

namespace CAServer.Grains.State.Chain;

[GenerateSerializer]
public class ChainState
{
	[Id(0)]
    public string Id { get; set; }
	[Id(1)]
    public string ChainId { get; set; }
	[Id(2)]
    public string ChainName { get; set; }
	[Id(3)]
    public string EndPoint { get; set; }
	[Id(4)]
    public string ExplorerUrl { get; set; }
	[Id(5)]
    public string CaContractAddress { get; set; }
	[Id(6)]
    public DateTime LastModifyTime { get; set; }
	[Id(7)]
    public DefaultToken DefaultToken { get; set; }
	[Id(8)]
    public bool IsDeleted { get; set; } = false;
}
