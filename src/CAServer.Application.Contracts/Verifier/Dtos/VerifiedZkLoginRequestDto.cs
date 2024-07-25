using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CAServer.CAAccount.Dtos;

namespace CAServer.Verifier.Dtos;

public class VerifiedZkLoginRequestDto : IValidatableObject
{
    [Required] public GuardianIdentifierType Type { get; set; }
    [Required] public string AccessToken { get; set; }
    [Required] public string VerifierId { get; set; }
    [Required] public string ChainId { get; set; }
    [Required] public OperationType OperationType { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var usedZk = GuardianIdentifierType.Google.Equals(Type) 
                     || GuardianIdentifierType.Apple.Equals(Type)
                     || GuardianIdentifierType.Facebook.Equals(Type);
        if (!usedZk)
        {
            yield return new ValidationResult(
                "Invalid input type.",
                new[] { "GuardianIdentifierType" }
            );
        }
    }
}