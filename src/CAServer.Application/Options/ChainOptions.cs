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
    public string ContractAddress { get; set; }
    public string PrivateKey { get; set; }
    public string TokenContractAddress { get; set; }
    public string RedPackageContractAddress { get; set; }
}