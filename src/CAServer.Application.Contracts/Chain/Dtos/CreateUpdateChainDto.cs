using System.ComponentModel.DataAnnotations;

namespace CAServer.Chain;

public class CreateUpdateChainDto
{
    [Required] public string ChainId { get; set; }
    [Required] public string ChainName { get; set; }
    [Required] public string EndPoint { get; set; }
    [Required] public string ExplorerUrl { get; set; }
    [Required] public string CaContractAddress { get; set; }
    public DefaultToken DefaultToken { get; set; }
}