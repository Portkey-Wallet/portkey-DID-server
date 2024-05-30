using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Market;

public class UserCollectFavoriteTokenDto : IValidatableObject
{
    [Required] public string Id { get; set; }
    
    [Required] public string Symbol { get; set; }
    
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        throw new System.NotImplementedException();
    }
}