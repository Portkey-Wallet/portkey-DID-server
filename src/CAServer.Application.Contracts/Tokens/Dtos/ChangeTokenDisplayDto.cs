using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Tokens.Dtos;

public class ChangeTokenDisplayDto : IValidatableObject
{
    public bool IsDisplay { get; set; }
    public List<string> Ids { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Ids.IsNullOrEmpty())
        {
            yield return new ValidationResult(
                "Invalid ids input.",
                new[] { "ids" }
            );
        }
    }
}