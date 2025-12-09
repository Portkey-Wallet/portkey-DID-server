using System.Collections.Generic;
using Newtonsoft.Json;

namespace CAServer.Tokens.TokenPrice.Provider.FeiXiaoHao;

public class FeiXiaoHaoResponseDto
{
    public List<FeiXiaoHaoTokenInfo> Data { get; set; }
}

public class FeiXiaoHaoTokenInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Symbol { get; set; }
    public int Rank { get; set; }
    public string Logo { get; set; }

    [JsonProperty(PropertyName = "logo_png")]
    public string LogoPng { get; set; }

    [JsonProperty(PropertyName = "price_usd")]
    public decimal PriceUsd { get; set; }

    [JsonProperty(PropertyName = "price_btc")]
    public decimal PriceBtc { get; set; }

    [JsonProperty(PropertyName = "volume_24h_usd")]
    public decimal Volume24HUsd { get; set; }

    [JsonProperty(PropertyName = "market_cap_usd")]
    public decimal MarketCapUsd { get; set; }

    [JsonProperty(PropertyName = "available_supply")]
    public decimal AvailableSupply { get; set; }

    [JsonProperty(PropertyName = "total_supply")]
    public decimal TotalSupply { get; set; }

    [JsonProperty(PropertyName = "max_supply")]
    public decimal MaxSupply { get; set; }

    [JsonProperty(PropertyName = "percent_change_1h")]
    public decimal PercentChange1H { get; set; }

    [JsonProperty(PropertyName = "percent_change_24h")]
    public decimal PercentChange24H { get; set; }

    [JsonProperty(PropertyName = "percent_change_7d")]
    public decimal PercentChange7D { get; set; }

    [JsonProperty(PropertyName = "last_updated")]
    public long LastUpdated { get; set; }
}