using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.CAAccount.Dtos;

public class TransactionFeeDto : IValidatableObject
{
    [Required] public List<string> ChainIds { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ChainIds?.Count == 0)
        {
            yield return new ValidationResult("Invalid ChainIds input.");
        }
    }
}