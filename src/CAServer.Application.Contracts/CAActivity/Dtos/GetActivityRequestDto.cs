using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.CAActivity.Dto;

public class GetActivityRequestDto : IValidatableObject
{
    [Required] public string TransactionId { get; set; }
    [Required] public string BlockHash { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(TransactionId))
        {
            yield return new ValidationResult("Invalid TransactionId input.");
        }

        if (string.IsNullOrEmpty(BlockHash))
        {
            yield return new ValidationResult("Invalid BlockHash input.");
        }
    }
}