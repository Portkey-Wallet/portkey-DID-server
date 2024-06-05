using System;

namespace CAServer.Market;

public class UserMarketTokenFavoritesDto
{
    public Guid UserId { get; set; }
    
    public string CoingeckoId { get; set; }
    
    public string Symbol { get; set; }
    
    public long CollectTimestamp { get; set; }
    
    public bool Collected { get; set; }
}