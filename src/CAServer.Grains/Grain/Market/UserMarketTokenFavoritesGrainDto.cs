using CAServer.Grains.State.Market;

namespace CAServer.Grains.Grain.Market;

[GenerateSerializer]
public class UserMarketTokenFavoritesGrainDto
{
    [Id(0)]
    public Guid UserId { get; set; }
    
    [Id(1)]
    public List<MarketToken> Favorites { get; set; }
}