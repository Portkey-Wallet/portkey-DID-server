using System;
using Orleans;

namespace CAServer.Market;

[GenerateSerializer]
public class UserMarketTokenFavoritesDto
{
    [Id(0)] public Guid UserId { get; set; }
    
    [Id(1)] public string CoingeckoId { get; set; }
    
    [Id(2)] public string Symbol { get; set; }
    
    [Id(3)] public long CollectTimestamp { get; set; }
    
    [Id(4)] public bool Collected { get; set; }
}