using Orleans;

namespace CAServer.Market;

[GenerateSerializer]
public class DefaultFavoriteDto
{
    [Id(0)] public string CoingeckoId { get; set; }
    
    [Id(1)] public string Symbol { get; set; }
    
    [Id(2)] public long CollectTimestamp { get; set; }
    
    [Id(3)] public bool Collected { get; set; }
}