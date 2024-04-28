using System.Collections.Generic;

namespace CAServer.Options;

public class ChainOptions
{
    public Dictionary<string, ChainInfo> ChainInfos { get; set; }
}

public class ChainInfo
{
    public string ChainId { get; set; }
    public string BaseUrl { get; set; }
    public decimal TransactionFee { get; set; } = 0.0041M;
    public string ContractAddress { get; set; }
    public string TokenContractAddress { get; set; }
    public string CrossChainContractAddress { get; set; }
    public string RedPackageContractAddress { get; set; }
    public string PublicKey { get; set; }
    public bool IsMainChain { get; set; }
}