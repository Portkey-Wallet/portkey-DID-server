using CAServer.FreeMint.Dtos;

namespace CAServer.Grains.State.FreeMint;

[GenerateSerializer]
public class FreeMintState
{
	[Id(0)]
    public string Id { get; set; }
	[Id(1)]
    public Guid UserId { get; set; }
	[Id(2)]
    public FreeMintCollectionInfo CollectionInfo { get; set; }
	[Id(3)]
    public List<ItemMintInfo> MintInfos { get; set; } = new();
	[Id(4)]
    public string PendingTokenId { get; set; }
	[Id(5)]
    public List<string> TokenIds { get; set; } = new();
	[Id(6)]
    public Dictionary<string, List<string>> DateMintInfo { get; set; } = new();
}
