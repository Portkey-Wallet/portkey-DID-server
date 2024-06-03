using CAServer.Grains.State.Market;

namespace CAServer.Grains.Grain.Market;

public class UserMarketTokenFavoritesGrainDto
{
    public Guid UserId { get; set; }
    
    public List<MarketToken> Favorites { get; set; }
}