using System.Collections.Generic;

namespace CAServer.UserAssets.Dtos;

public class IndexerNftItemInfos
{
    public List<NftItemInfo> NftItemInfos { get; set; }
}

public class IndexerNftItemWithTraitsInfos
{
    public List<NftItemInfo> NftItemWithTraitsInfos { get; set; }
}

public class NftItemInfo
{
    public string Symbol { get; set; }
    public string TokenContractAddress { get; set; }
    public int Decimals { get; set; }
    public long Supply { get; set; }
    public long TotalSupply { get; set; }
    public string TokenName { get; set; }
    public string Issuer { get; set; }
    public bool IsBurnable { get; set; }
    public int IssueChainId { get; set; }
    public string ImageUrl { get; set; }
    public string CollectionSymbol { get; set; }
    public string CollectionName { get; set; }
    public string Traits { get; set; }

}