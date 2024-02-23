using System.Collections.Generic;

namespace CAServer.UserAssets.Dtos;

public class SearchUserPackageAssetsDto
{
    
    public List<UserPackageAsset> Data { get; set; }
    
    public long TotalRecordCount { get; set; }
    
    public long FtRecordCount { get; set; }
    
    public long NftRecordCount { get; set; }
}

public class UserPackageAsset
{
    public string ChainId { get; set; }
    
    public string Symbol { get; set; }
    
    public string Address { get; set; }

    public string Decimals { get; set; }
    
    public string ImageUrl { get; set; }
    
    public int AssetType { get; set; }
    
    public string Alias { get; set; }
    
    public string TokenId { get; set; } 
}

public enum AssetType
{
    FT = 1,
    NFT = 2
}