using CAServer.FreeMint.Dtos;

namespace CAServer.Grains.Grain.FreeMint;

[GenerateSerializer]
public class FreeMintGrainDto
{
    [Id(0)]
    public Guid UserId { get; set; }
    
    [Id(1)]
    public FreeMintCollectionInfo CollectionInfo { get; set; }
    
    [Id(2)]
    public List<ItemMintInfo> MintInfos { get; set; }
}