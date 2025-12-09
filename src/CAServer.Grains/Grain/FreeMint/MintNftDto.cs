using CAServer.FreeMint.Dtos;

namespace CAServer.Grains.Grain.FreeMint;

[GenerateSerializer]
public class MintNftDto
{
    [Id(0)]
    public FreeMintCollectionInfo CollectionInfo { get; set; }
    
    [Id(1)]
    public ConfirmGrainDto ConfirmInfo { get; set; }
}

[GenerateSerializer]
public class ConfirmGrainDto
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
}