using System.Collections.Generic;
using CAServer.Commons.Etos;
using Org.BouncyCastle.Asn1.Crmf;

namespace CAServer.UserAssets.Dtos;

public class SearchUserAssetsDto
{
    public List<UserAsset> Data { get; set; }
    public long TotalRecordCount { get; set; }
}

public class SearchUserAssetsV2Dto
{
    public List<TokenInfoV2Dto> TokenInfos { get; set; }
    public List<NftCollectionDto> NftInfos { get; set; }
    public long TotalRecordCount { get; set; }
}

public class UserAsset : ChainDisplayNameDto
{
    public string Symbol { get; set; }
    public string Address { get; set; }
    public TokenInfoDto TokenInfo { get; set; }
    public NftInfoDto NftInfo { get; set; }
    public string Label { get; set; }
}

public class TokenInfoDto : ChainDisplayNameDto
{
    public string Balance { get; set; }
    public string Decimals { get; set; }
    public string BalanceInUsd { get; set; }
    public string TokenContractAddress { get; set; }
    public string ImageUrl { get; set; }
}

public class NftInfoDto
{
    public string ImageUrl { get; set; }
    public string Alias { get; set; }
    public string TokenId { get; set; }
    public string CollectionName { get; set; }
    public string Balance { get; set; }
    public string TokenContractAddress { get; set; }
    public string Decimals { get; set; }
    public string TokenName { get; set; }
    public bool IsSeed { get; set; }
    public int SeedType { get; set; }
    public string DisplayChainName { get; set; }
    public string ChainImageUrl { get; set; }
    public string Label { get; set; }
}

public class TokenInfoV2Dto : TokenInfoDto
{
    public string Symbol { get; set; }
    public string Address { get; set; }
    public string Label { get; set; }
}

public class NftCollectionDto
{
    public string CollectionName { get; set; }
    public string ImageUrl { get; set; }
    public List<NftInfoDto> Items { get; set; }
}