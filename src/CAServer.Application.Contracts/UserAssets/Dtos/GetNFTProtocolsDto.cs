using System.Collections.Generic;

namespace CAServer.UserAssets.Dtos;

public class GetNftCollectionsDto
{
    public List<NftCollection> Data { get; set; }
    public long TotalRecordCount { get; set; }
}

public class NftCollection
{
    public string ImageUrl { get; set; }
    public string CollectionName { get; set; }
    public int ItemCount { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }
}