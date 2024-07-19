using System;
using CAServer.FreeMint.Dtos;
using Volo.Abp.EventBus;

namespace CAServer.FreeMint.Etos;

[EventName("FreeMintEto")]
public class FreeMintEto
{
    public Guid UserId { get; set; }
    public FreeMintCollectionInfo CollectionInfo { get; set; }
    public ItemMintInfo ConfirmInfo { get; set; }
}