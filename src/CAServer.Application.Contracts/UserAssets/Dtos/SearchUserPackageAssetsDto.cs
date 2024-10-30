using System.Collections.Generic;
using CAServer.Commons.Etos;

namespace CAServer.UserAssets.Dtos;

public class SearchUserPackageAssetsDto
{
    
    public List<UserPackageAsset> Data { get; set; }
    
    public long TotalRecordCount { get; set; }
    
    public long FtRecordCount { get; set; }
    
    public long NftRecordCount { get; set; }
}

public class UserPackageAsset : ChainDisplayNameDto
{
    public string Symbol { get; set; }

    public string Decimals { get; set; }
    
    public string ImageUrl { get; set; }
    
    public int AssetType { get; set; }
    
    public string Alias { get; set; }
    
    public string TokenId { get; set; } 
    
    public string Balance { get; set; }
    
    public string TokenContractAddress { get; set; }
    
    public string TokenName { get; set; }
    
    public bool IsSeed { get; set; }
    
    public int SeedType { get; set; }  
    public bool IsDisplay { get; set; }
    public string Label { get; set; }
}

public enum AssetType
{
    FT = 1,
    NFT = 2
}