namespace CAServer.Options;

public class ExchangeOptions
{
    
    public BinanceOptions Binance {get; set; }
    
    public OkxOptions Okx { get; set; }
    
}


public class BinanceOptions
{
    public string BaseUrl { get; set; }
    public int Block429Seconds { get; set; } = 300;
}

public class OkxOptions
{
    public string BaseUrl { get; set; }
}