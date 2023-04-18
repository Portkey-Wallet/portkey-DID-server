using System.Collections.Generic;

namespace CAServer.UserAssets.Dtos;

public class GetNFTItemsDto
{
    public List<NftItem> Data { get; set; }
}
public class NftItem
{
    public string Symbol { get; set; }
    public string ChainId { get; set; }
    public long TokenId { get; set; }
    public string Alias { get; set; }
    public string Balance { get; set; }
    public string ImageUrl { get; set; }
}