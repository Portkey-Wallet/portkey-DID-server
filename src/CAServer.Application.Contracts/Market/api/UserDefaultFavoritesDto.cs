using System;
using System.Collections.Generic;
using Orleans;

namespace CAServer.Market;

[GenerateSerializer]
public class UserDefaultFavoritesDto
{
    [Id(0)] public Guid UserId { get; set; }
    
    [Id(1)] public List<DefaultFavoriteDto> DefaultFavorites { get; set; }
}