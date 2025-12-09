using System.Collections.Generic;

namespace CAServer.UserAssets.Provider;

public class IndexerSearchTokenNfts
{
    public CaHolderSearchTokenNFT CaHolderSearchTokenNFT { get; set; }
}

public class CaHolderSearchTokenNFT
{
    public List<IndexerSearchTokenNft> Data { get; set; }
    public long TotalRecordCount { get; set; }
}

public class IndexerSearchTokenNft
{
    public string ChainId { get; set; }
    public string CaAddress { get; set; }
    public long Balance { get; set; }
    public long TokenId { get; set; }
    public TokenInfo TokenInfo { get; set; }
    public NftInfo NftInfo { get; set; }
}

public enum TokenType
{
    TOKEN,
    NFT_COLLECTION,
    NFT_ITEM
}