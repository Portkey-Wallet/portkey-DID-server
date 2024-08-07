using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Contacts;

public class CreateUpdateContactDto : IValidatableObject
{
    [RegularExpression(@"^[a-zA-Z\d'_'' '\s]{1,16}$")]
    public string Name { get; set; }

    public string RelationId { get; set; }

    public List<ContactAddressDto> Addresses { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Name.IsNullOrWhiteSpace() && RelationId.IsNullOrWhiteSpace())
        {
            yield return new ValidationResult("Invalid input.");
        }

        if (!RelationId.IsNullOrWhiteSpace())
        {
            if (Addresses is { Count: > 0 })
            {
                yield return new ValidationResult("Invalid input.");
            }
        }
        else
        {
            if (Addresses?.Count != 1)
            {
                yield return new ValidationResult("Invalid input.");
            }
        }
    }
}