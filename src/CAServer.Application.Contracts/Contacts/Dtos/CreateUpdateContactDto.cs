using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Contacts;

public class CreateUpdateContactDto : IValidatableObject
{
    [RegularExpression(@"^[a-zA-Z\d'_'' '\s]{1,16}$")]
    public string Name { get; set; }

    public string RelationId { get; set; }

    [ValidAddresses] public List<ContactAddressDto> Addresses { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Name.IsNullOrWhiteSpace() && RelationId.IsNullOrWhiteSpace())
        {
            yield return new ValidationResult("Invalid input.");
        }
    }
}