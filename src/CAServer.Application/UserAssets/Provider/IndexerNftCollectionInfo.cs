using System.Collections.Generic;

namespace CAServer.UserAssets.Provider;

public class IndexerNftCollectionInfos
{
    public CaHolderNFTCollectionBalanceInfo CaHolderNFTCollectionBalanceInfo { get; set; }
}

public class CaHolderNFTCollectionBalanceInfo
{
    public List<IndexerNftCollectionInfo> Data { get; set; }
    public long TotalRecordCount { get; set; }
    public long TotalItemCount { get; set; }
}

public class IndexerNftCollectionInfo
{
    public string CaAddress { get; set; }
    public string ChainId { get; set; }
    public List<long> TokenIds { get; set; }
    public NftCollectionInfo NftCollectionInfo { get; set; }
}

public class NftCollectionInfo
{
    public string Symbol { get; set; }
    public int Decimals { get; set; }
    public string TokenName { get; set; }
    public string ImageUrl { get; set; }
    public long TotalSupply { get; set; }
}