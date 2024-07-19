using CAServer.FreeMint.Dtos;

namespace CAServer.Grains.State.FreeMint;

public class FreeMintState
{
    public string Id { get; set; }
    public Guid UserId { get; set; }
    public FreeMintCollectionInfo CollectionInfo { get; set; }
    public List<ItemMintInfo> MintInfos { get; set; } = new();
    public string UnUsedTokenId { get; set; }
    public string PendingTokenId { get; set; }
}