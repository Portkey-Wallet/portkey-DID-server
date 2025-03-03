namespace CAServer.Grains.State.Market;

[GenerateSerializer]
public class UserMarketTokenFavoritesState
{
	[Id(0)]
    public Guid Id { get; set; }
	[Id(1)]
    public Guid UserId { get; set; }

	[Id(2)]
    public List<MarketToken> Favorites { get; set; } = new();
}

[GenerateSerializer]
public class MarketToken
{
	[Id(0)]
    public string CoingeckoId { get; set; }
    
	[Id(1)]
    public string Symbol { get; set; }
    
	[Id(2)]
    public long CollectTimestamp { get; set; }
    
	[Id(3)]
    public bool Collected { get; set; }
}
