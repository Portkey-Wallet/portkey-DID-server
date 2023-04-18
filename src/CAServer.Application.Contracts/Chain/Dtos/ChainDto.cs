using System;

namespace CAServer.Chain;

public class ChainDto
{
    public string ChainId { get; set; }
    public string ChainName { get; set; }
    public string EndPoint { get; set; }
    public string ExplorerUrl { get; set; }
    public string CaContractAddress { get; set; }
    public DateTime LastModifyTime { get; set; }
}