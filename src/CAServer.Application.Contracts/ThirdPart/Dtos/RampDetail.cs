namespace CAServer.ThirdPart.Dtos;

public class RampDetail
{
    public string Price { get; set; }
    public CryptoExchange CryptoExchange { get; set; }
    
}

public class CryptoExchange
{
    public string FromCrypto { get; set; }
    public string ToFiat { get; set; }
    public string Price { get; set; }
}