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
    public DefaultToken DefaultToken { get; set; }
}

public class DefaultToken
{
    public string Name { get; set; }
    public string Address { get; set; }
    public string ImageUrl { get; set; }
    public string Symbol { get; set; }
    public string Decimals { get; set; }
}