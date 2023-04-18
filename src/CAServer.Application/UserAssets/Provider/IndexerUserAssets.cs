using System.Collections.Generic;

namespace CAServer.UserAssets.Provider;

public class IndexerUserAssets
{
    public List<IndexerUserAsset> caHolderSearchTokenNFT { get; set; }
}

public class IndexerUserAsset
{
    public string ChainId { get; set; }
    public string CaAddress { get; set; }
    public long Balance { get; set; }
    public IndexerTokenInfo IndexerTokenInfo { get; set; }
    public NFTInfo NftInfo { get; set; }
}

public class IndexerTokenInfo
{
    public string Symbol { get; set; }
    public string TokenContractAddress { get; set; }
    public int Decimals { get; set; }
}

public class NFTInfo
{
    public string Symbol { get; set; }
    public string NftContractAddress { get; set; }
    public string ProtocolName { get; set; }
    public long TokenId { get; set; }
    public long Quantity { get; set; }
    public string Alias { get; set; }
    public string Uri { get; set; }
}