using System.ComponentModel.DataAnnotations;

namespace CAServer.Market;

public class UserCollectFavoriteTokenDto
{
    [Required] public string Id { get; set; }
    
    [Required] public string Symbol { get; set; }
    
}