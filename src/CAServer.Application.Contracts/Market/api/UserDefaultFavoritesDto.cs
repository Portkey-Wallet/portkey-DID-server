using System;
using System.Collections.Generic;

namespace CAServer.Market;

public class UserDefaultFavoritesDto
{
    public Guid UserId { get; set; }
    
    public List<DefaultFavoriteDto> DefaultFavorites { get; set; }
}