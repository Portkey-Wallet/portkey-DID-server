using CAServer.FreeMint.Dtos;
using CAServer.Grains.State.FreeMint;

namespace CAServer.Grains.Grain.FreeMint;

public class FreeMintGrainDto
{
    public Guid UserId { get; set; }
    public FreeMintCollectionInfo CollectionInfo { get; set; }
    public List<ItemMintInfo> MintInfos { get; set; }
}