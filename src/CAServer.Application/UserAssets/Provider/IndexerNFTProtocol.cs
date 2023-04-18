using System.Collections.Generic;

namespace CAServer.UserAssets.Provider;

public class IndexerNFTProtocol
{
    public List<NftProtocol> userNFTProtocolInfo { get; set; }
}

public class NftProtocol
{
    public string ChainId { get; set; }
    public List<long> TokenIds { get; set; }
    public NFTProtocolDto NftProtocolInfo { get; set; }
}

public class NFTProtocolDto
{
    public string Symbol { get; set; }
    public string NftType { get; set; }
    public string ProtocolName { get; set; }
    public long? Supply { get; set; }
    public long TotalSupply { get; set; }
    public int IssueChainId { get; set; }
    public string ImageUrl { get; set; }
}