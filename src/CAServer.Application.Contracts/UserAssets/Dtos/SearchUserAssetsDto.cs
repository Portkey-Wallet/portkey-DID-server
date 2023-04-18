using System.Collections.Generic;
using Org.BouncyCastle.Asn1.Crmf;

namespace CAServer.UserAssets.Dtos;

public class SearchUserAssetsDto
{
    public List<UserAsset> Data { get; set; }
}

public class UserAsset
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public string Address { get; set; }

    public TokenInfo TokenInfo { get; set; }
    public NftInfo NftInfo { get; set; }
}

public class TokenInfo
{
    public string Balance { get; set; }
    public string Decimal { get; set; }
    public string BalanceInUsd { get; set; }
}

public class NftInfo
{
    public string ImageUrl { get; set; }
    public string Alias { get; set; }
    public string TokenId { get; set; }
    public string ProtocolName { get; set; }
    public string Quantity { get; set; }
}