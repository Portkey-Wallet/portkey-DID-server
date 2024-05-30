using System;
using System.Collections.Generic;

namespace CAServer.Market;

public class MarketCryptocurrencyDto
{
    public string Id { get; set; }
    public string Symbol { get; set; }
    public string Name { get; set; }
    public string Image { get; set; }
    public Decimal? TotalSupply { get; set; }
    public Decimal? TotalVolume { get; set; }
    public string Description { get; set; }
    public bool SupportEtransfer { get; set; }
    public bool Collected { get; set; }
    public Decimal? CurrentPrice { get; set; }
    public Decimal? OriginalCurrentPrice { get; set; }
    
    public Decimal? OriginalMarketCap { get; set; }
    
    public Decimal? PriceChangePercentage24HInCurrency { get; set; }
    
    public double PriceChangePercentage24H { get; set; }
    
    public Decimal? PriceChange24H { get; set; }
    
    public string MarketCap { get; set; }

    public DateTimeOffset LastUpdated { get; set; }
}