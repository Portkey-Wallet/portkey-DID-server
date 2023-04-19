using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Guardian;

public class GuardianIdentifierDto : IValidatableObject
{
   public string GuardianIdentifier { get; set; }
   [Required] public string ChainId { get; set; }
    public string CaHash { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(GuardianIdentifier) && string.IsNullOrWhiteSpace(CaHash))
        {
            yield return new ValidationResult(
                "Invalid type input.",
                new[] { "guardianIdentifier, caHash can not both empty or whitespace." }
            );
        }
    }
}