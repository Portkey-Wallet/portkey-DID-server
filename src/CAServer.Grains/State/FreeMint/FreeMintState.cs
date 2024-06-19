using CAServer.EnumType;
using CAServer.FreeMint.Dtos;

namespace CAServer.Grains.State.FreeMint;

public class FreeMintState
{
    public string Id { get; set; }
    public Guid UserId { get; set; }
    public FreeMintCollectionInfo CollectionInfo { get; set; }
    public List<ItemMintInfo> MintInfos { get; set; }
}

public class ItemMintInfo
{
    public string ItemId { get; set; }
    public string ImageUrl { get; set; }
    public string Name { get; set; }
    public string TokenId { get; set; }
    public string Description { get; set; }
    public FreeMintStatus Status { get; set; }
}