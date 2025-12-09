namespace CAServer.Grains.Grain.CryptoGift;

[GenerateSerializer]
public class CryptoGiftCacheDto
{
    [Id(0)]
    public bool Success { get; set; }
    
    [Id(1)]
    public bool IsNewUser { get; set; }
    
    [Id(2)]
    public CryptoGiftDto CryptoGiftDto { get; set; }
}