using System;
using CAServer.FreeMint.Dtos;

namespace CAServer.FreeMint.Etos;

public class FreeMintEto
{
    public Guid UserId { get; set; }
    public FreeMintCollectionInfo CollectionInfo { get; set; }
    public FreeMintNftItemInfo ConfirmInfo { get; set; }
}

public class FreeMintNftItemInfo
{
    public string ItemId { get; set; }
    public string ImageUrl { get; set; }
    public string Name { get; set; }
    public string TokenId { get; set; }
    public string Description { get; set; }
}