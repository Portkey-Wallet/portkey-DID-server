using System.Collections.Generic;
using CAServer.Commons.Etos;

namespace CAServer.UserAssets.Dtos;

public class GetNftItemsDto
{
    public List<NftItem> Data { get; set; }
    public long TotalRecordCount { get; set; }
}
public class NftItem : ChainDisplayNameDto
{
    public string Symbol { get; set; }
    public string TokenId { get; set; }
    public string Alias { get; set; }
    public string Balance { get; set; }
    public long TotalSupply { get; set; }
    public long CirculatingSupply { get; set; }
    public string ImageUrl { get; set; }
    public string TokenContractAddress { get; set; }
    public string ImageLargeUrl { get; set; }
    public string Decimals { get; set; }
    public string CollectionSymbol { get; set; }
    public string InscriptionName { get; set; }
    public string LimitPerMint {get; set;}
    public string Expires { get; set; }
    public string SeedOwnedSymbol { get; set; }
    public string Generation { get; set; }
    public string Traits { get; set; }
    public List<Trait> TraitsPercentages { get; set; }
    public string TokenName { get; set; }
    public bool IsSeed { get; set; }
    public int SeedType { get; set; }
    public int RecommendedRefreshSeconds { get; set; }
}

public enum SeedType
{
    FT = 1,
    NFT = 2
}

public class Trait
{
    public string TraitType { get; set; }
    public string Value { get; set; }
    public string Percent { get; set; }
}