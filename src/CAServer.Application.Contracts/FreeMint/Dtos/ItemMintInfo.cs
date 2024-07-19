using CAServer.EnumType;

namespace CAServer.FreeMint.Dtos;

public class ItemMintInfo
{
    public string ItemId { get; set; }
    public string ImageUrl { get; set; }
    public string Name { get; set; }
    public string TokenId { get; set; }
    public string Description { get; set; }
    public FreeMintStatus Status { get; set; }
}