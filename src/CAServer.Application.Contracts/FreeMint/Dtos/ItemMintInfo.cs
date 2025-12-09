using CAServer.EnumType;
using Orleans;

namespace CAServer.FreeMint.Dtos;

[GenerateSerializer]
public class ItemMintInfo
{
    [Id(0)]
    public string ItemId { get; set; }

    [Id(1)]
    public string ImageUrl { get; set; }

    [Id(2)]
    public string Name { get; set; }

    [Id(3)]
    public string TokenId { get; set; }

    [Id(4)]
    public string Description { get; set; }

    [Id(5)]
    public FreeMintStatus Status { get; set; }
}