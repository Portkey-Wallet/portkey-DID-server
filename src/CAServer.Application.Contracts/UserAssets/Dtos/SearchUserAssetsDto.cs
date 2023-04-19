using System.Collections.Generic;
using Org.BouncyCastle.Asn1.Crmf;

namespace CAServer.UserAssets.Dtos;

public class SearchUserAssetsDto
{
    public List<UserAsset> Data { get; set; }
    public long TotalRecordCount { get; set; }
}

public class UserAsset
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public string Address { get; set; }

    public TokenInfoDto TokenInfo { get; set; }
    public NftInfoDto NftInfo { get; set; }
}

public class TokenInfoDto
{
    public string Balance { get; set; }
    public string Decimals { get; set; }
    public string BalanceInUsd { get; set; }
    public string TokenContractAddress { get; set; }
}

public class NftInfoDto
{
    public string ImageUrl { get; set; }
    public string Alias { get; set; }
    public string TokenId { get; set; }
    public string CollectionName { get; set; }
    public string Balance { get; set; }
    public string TokenContractAddress { get; set; }
}