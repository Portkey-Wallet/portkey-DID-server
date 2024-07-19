using System;
using CAServer.FreeMint.Dtos;

namespace CAServer.FreeMint.Etos;

public class FreeMintEto
{
    public Guid UserId { get; set; }
    public FreeMintCollectionInfo CollectionInfo { get; set; }
    public ItemMintInfo ConfirmInfo { get; set; }
}