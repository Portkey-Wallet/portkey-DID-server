using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CAServer.CAAccount.Dtos;

namespace CAServer.Verifier.Dtos;

public class VerifiedZkLoginRequestDto : IValidatableObject
{
    [Required] public GuardianIdentifierType Type { get; set; }
    [Required] public string AccessToken { get; set; }
    public string VerifierId { get; set; }
    [Required] public string ChainId { get; set; }
    [Required] public OperationType OperationType { get; set; }
    public string Jwt { get; set; }

    [Required] public string Salt { get; set; }
    
    [Required] public string PoseidonIdentifierHash { get; set; }

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

        if (Type == GuardianIdentifierType.Facebook && Jwt.IsNullOrEmpty())
        {
            yield return new ValidationResult(
                "Invalid input jwt.",
                new[] { "Jwt" }
            );
        }

        if (OperationType.CreateCAHolder.Equals(OperationType) || OperationType.AddGuardian.Equals(OperationType))
        {
            if (PoseidonIdentifierHash.IsNullOrEmpty())
            {
                yield return new ValidationResult(
                    "Invalid guardianIdentifierHash when CreateCAHolder and AddGuardian.",
                    new[] { "PoseidonIdentifierHash" }
                );
            }
            if (Salt.IsNullOrEmpty())
            {
                yield return new ValidationResult(
                    "Invalid salt when CreateCAHolder and AddGuardian.",
                    new[] { "salt" }
                );
            }
        }
    }
}