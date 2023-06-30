using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Tokens.Dtos;

public class GetTokenListRequestDto : IValidatableObject
{
    [Required] public string Symbol { get; set; }
    public List<string> ChainIds { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ChainIds == null || ChainIds.Count == 0)
        {
            yield return new ValidationResult(
                "Invalid type input.",
                new[] { "ChainIds" }
            );
        }
    }
}