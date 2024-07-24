using CAServer.FreeMint.Dtos;

namespace CAServer.Grains.State.FreeMint;

public class FreeMintState
{
    public string Id { get; set; }
    public Guid UserId { get; set; }
    public FreeMintCollectionInfo CollectionInfo { get; set; }
    public List<ItemMintInfo> MintInfos { get; set; } = new();
    public string PendingTokenId { get; set; }
    public List<string> TokenIds { get; set; } = new();
    public Dictionary<string, List<string>> DateMintInfo { get; set; } = new();
}