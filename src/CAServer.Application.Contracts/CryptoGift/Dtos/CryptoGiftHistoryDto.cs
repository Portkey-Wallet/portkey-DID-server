using System.Collections.Generic;

namespace CAServer.RedPackage.Dtos;

public class CryptoGiftHistoryDto
{
    public RedPackageDetailDto FirstDetail { get; set; }

    public List<CryptoGiftHistoryItemDto> Histories { get; set; } = new List<CryptoGiftHistoryItemDto>();
}