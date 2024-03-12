using System.Collections.Generic;

namespace CAServer.UserAssets.Provider;

public class IndexerNftInfos
{
    public CaHolderNFTBalanceInfo CaHolderNFTBalanceInfo { get; set; }
}

public class CaHolderNFTBalanceInfo
{
    public List<IndexerNftInfo> Data { get; set; }
    public long TotalRecordCount { get; set; }
}

public class IndexerNftInfo
{
    public string CaAddress { get; set; }
    public string ChainId { get; set; }
    public long Balance { get; set; }
    public NftInfo NftInfo { get; set; }
}

public class NftInfo
{
    public string Symbol { get; set; }
    public long Decimals { get; set; }
    public string ImageUrl { get; set; }
    public string CollectionSymbol { get; set; }
    public string CollectionName { get; set; }
    public string TokenName { get; set; }
    public long TotalSupply { get; set; }
    public long Supply { get; set; }
    public string TokenContractAddress { get; set; }
    public string InscriptionName { get; set; }
    public string Lim {get; set;}
    public string Expires { get; set; }
    public string SeedOwnedSymbol { get; set; }
    public string Generation { get; set; }
    public string Traits { get; set; }
}