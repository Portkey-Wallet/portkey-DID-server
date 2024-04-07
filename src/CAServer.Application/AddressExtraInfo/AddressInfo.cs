using System.Collections.Generic;

namespace CAServer.AddressExtraInfo;

public class AddressInfo
{
    public string CaAddress { get; set; }
    public string CaHash { get; set; }
    public string OriginChainId { get; set; }
    public List<string> Identifiers { get; set; }
}

public class AddressInfoExtension : AddressInfo
{
    public string ChainId { get; set; }
}