using System.Collections.Generic;
using CAServer.RedPackage.Dtos;

namespace CAServer.CryptoGift.Dtos;

public class PreGrabbedDto
{
    public List<PreGrabbedItemDto> Items { get; set; } = new List<PreGrabbedItemDto>();
}