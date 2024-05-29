using System.Collections.Generic;
using Newtonsoft.Json;

namespace CAServer.Market;

public class CryptocurrencyQuotesLatestDto
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Symbol { get; set; }
    public string Slug { get; set; }
    
    [JsonProperty(PropertyName = "cmc_rank")]
    public int CmcRank { get; set; }
    
    [JsonProperty(PropertyName = "num_market_pairs")]
    public int NumMarketPairs { get; set; }
    
    [JsonProperty(PropertyName = "circulating_supply")]
    public int CirculatingSupply { get; set; }
    
    [JsonProperty(PropertyName = "total_supply")]
    public int TotalSupply { get; set; }
    
    [JsonProperty(PropertyName = "max_supply")]
    public int MaxSupply { get; set; }
    
    [JsonProperty(PropertyName = "last_updated")]
    public string LastUpdated { get; set; }
    
    [JsonProperty(PropertyName = "date_added")]
    public string DateAdded { get; set; }
    
    public List<string> Tags { get; set; }
    
    public Dictionary<string, CryptocurrencyListingsQuoteDataDto> quote { get; set; }
    

}