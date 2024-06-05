namespace CAServer.Market;

public class MarketTokenDto
{
    public string CoingeckoId { get; set; }
    
    public string Symbol { get; set; }
    
    public long CollectTimestamp { get; set; }
    
    public bool Collected { get; set; }
}