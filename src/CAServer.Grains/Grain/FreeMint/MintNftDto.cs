using CAServer.FreeMint.Dtos;

namespace CAServer.Grains.Grain.FreeMint;

public class MintNftDto
{
    public FreeMintCollectionInfo CollectionInfo { get; set; }
    public ConfirmGrainDto ConfirmInfo { get; set; }
}

public class ConfirmGrainDto
{
    public string ItemId { get; set; }
    public string ImageUrl { get; set; }
    public string Name { get; set; }
    public string TokenId { get; set; }
    public string Description { get; set; }
}