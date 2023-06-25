using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Verifier;

public class VerificationSignatureRequestDto : IValidatableObject
{
    [Required] public string VerifierSessionId { get; set; }
    [Required] public string VerificationCode { get; set; }
    [Required] public string GuardianIdentifier { get; set; }

    [Required] public string VerifierId { get; set; }
    [Required] public string ChainId { get; set; }
    
    [Required] public VerifierCodeOperationType VerifierCodeOperationType {get;set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (VerifierSessionId.IsNullOrEmpty() || VerifierId.IsNullOrEmpty() ||
            VerificationCode.IsNullOrEmpty() || GuardianIdentifier.IsNullOrEmpty() || ChainId.IsNullOrEmpty())
        {
            yield return new ValidationResult("Input is null or empty");
        }
    }
}