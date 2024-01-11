using CAServer.ThirdPart;

namespace CAServer.Grains.Grain.ThirdPart;

public class ExchangePriceDto
{

    public string FromCrypto { get; set; }
    public string ToCrypto { get; set; }
    public decimal Price { get; set; }
    public Dictionary<string, FeeItem> NetworkFee { get; set; } = new();


}