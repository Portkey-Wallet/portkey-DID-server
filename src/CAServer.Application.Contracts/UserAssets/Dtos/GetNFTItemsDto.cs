using System.Collections.Generic;

namespace CAServer.UserAssets.Dtos;

public class GetNftItemsDto
{
    public List<NftItem> Data { get; set; }
    public long TotalRecordCount { get; set; }
}
public class NftItem
{
    public string Symbol { get; set; }
    public string ChainId { get; set; }
    public string TokenId { get; set; }
    public string Alias { get; set; }
    public string Balance { get; set; }
    public long TotalSupply { get; set; }
    public long CirculatingSupply { get; set; }
    public string ImageUrl { get; set; }
    public string TokenContractAddress { get; set; }
    public string ImageLargeUrl { get; set; }
    
    public long IssueChainId { get; set; }
}