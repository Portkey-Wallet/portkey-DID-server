using Newtonsoft.Json;

namespace CAServer.Market;

public class CryptocurrencyListingsQuoteDataDto
{
    public float Price { get; set; }
        
    [JsonProperty(PropertyName = "volume_24h")]
    public int Volume24h { get; set; }
        
    [JsonProperty(PropertyName = "volume_change_24h")]
    public float VolumeChange24h { get; set; }
        
    [JsonProperty(PropertyName = "percent_change_1h")]
    public float PercentChange1h { get; set; }
        
    [JsonProperty(PropertyName = "percent_change_24h")]
    public float PercentChange24h { get; set; }
        
    [JsonProperty(PropertyName = "percent_change_7d")]
    public float PercentChange7d { get; set; }
        
    [JsonProperty(PropertyName = "market_cap")]
    public float MarketCap { get; set; }
        
    [JsonProperty(PropertyName = "market_cap_dominance")]
    public float MarketCapDominance { get; set; }
        
    [JsonProperty(PropertyName = "fully_diluted_market_cap")]
    public float FullyDilutedMarketCap { get; set; }
        
    [JsonProperty(PropertyName = "last_updated")]
    public string LastUpdated { get; set; }
}