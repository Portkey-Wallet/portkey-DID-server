using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CAServer.CAActivity.Dto;

public class GetActivityRequestDto : IValidatableObject
{
    [Required] public string TransactionId { get; set; }
    [Required] public string BlockHash { get; set; }
    public string TransactionType { get; set; }

    [Required] public List<string> CaAddresses { get; set; }

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

        if (CaAddresses == null || CaAddresses.Count == 0 || CaAddresses.Any(string.IsNullOrEmpty))
        {
            yield return new ValidationResult("Invalid CaAddresses input.");
        }
    }
}