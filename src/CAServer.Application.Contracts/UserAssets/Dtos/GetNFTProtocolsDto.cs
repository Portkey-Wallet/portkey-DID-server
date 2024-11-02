using System.Collections.Generic;
using CAServer.Commons.Etos;

namespace CAServer.UserAssets.Dtos;

public class GetNftCollectionsDto
{
    public List<NftCollection> Data { get; set; }
    public long TotalRecordCount { get; set; }
}

public class NftCollection : ChainDisplayNameDto
{
    public string ImageUrl { get; set; }
    public string CollectionName { get; set; }
    public int ItemCount { get; set; }
    public string Symbol { get; set; }
    public bool IsSeed { get; set; }
}