namespace CAServer.Market;

public class MarketCryptocurrencyQuoteDto
{
    public float Price { get; set; }
        
    public int Volume24h { get; set; }
    
    public float PercentChange24h { get; set; }
    
    public float MarketCap { get; set; }

    public string LastUpdated { get; set; }
}