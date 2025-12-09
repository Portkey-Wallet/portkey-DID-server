using System.Collections.Generic;

namespace CAServer.ThirdPart;

public class TreasuryPriceDto
{

    public string Crypto { get; set; }
    public string PriceSymbol { get; set; }
    public decimal Price { get; set; }
    public Dictionary<string, FeeItem> NetworkFee { get; set; } = new();


}