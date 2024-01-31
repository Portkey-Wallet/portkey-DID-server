using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Facebook.Dtos;

public class FacebookAuthDto : IValidatableObject
{
    public string Code { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            yield return new ValidationResult(
                "Invalid input.",
                new[] { "Code" }
            );
        }
    }
    
}

