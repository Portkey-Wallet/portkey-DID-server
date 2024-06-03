namespace CAServer.Grains.State.Market;

public class UserMarketTokenFavoritesState
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public List<MarketToken> Favorites { get; set; } = new();
}

public class MarketToken
{
    public string CoingeckoId { get; set; }
    
    public string Symbol { get; set; }
    
    public long CollectTimestamp { get; set; }
    
    public bool Collected { get; set; }
}