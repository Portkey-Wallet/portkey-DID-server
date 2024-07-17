using CAServer.FreeMint.Dtos;

namespace CAServer.Grains.Grain.FreeMint;

public class MintNftDto
{
    public Guid UserId { get; set; }
    public FreeMintCollectionInfo CollectionInfo { get; set; }
    
}