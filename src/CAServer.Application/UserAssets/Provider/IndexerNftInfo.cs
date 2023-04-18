using System.Collections.Generic;

namespace CAServer.UserAssets.Provider;

public class IndexerNftInfo
{
    public List<UserNftInfo> UserNFTInfo { get; set; }
}

public class UserNftInfo
{
    public string ChainId { get; set; }
    public long Balance { get; set; }
    public NftInfoDto NftInfo { get; set; }
}

public class NftInfoDto
{
    public string Symbol { get; set; }
    public long TokenId { get; set; }
    public string Alias { get; set; }
    public string ImageUrl { get; set; }
}