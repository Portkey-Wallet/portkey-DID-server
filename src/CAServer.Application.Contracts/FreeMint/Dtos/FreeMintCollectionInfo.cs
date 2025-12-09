using Orleans;

namespace CAServer.FreeMint.Dtos;

[GenerateSerializer]
public class FreeMintCollectionInfo
{
    [Id(0)]
    public string CollectionName { get; set; }
    
    [Id(1)]
    public string ImageUrl { get; set; }
    
    [Id(2)]
    public string ChainId { get; set; }
    
    [Id(3)]
    public string Symbol { get; set; }
}