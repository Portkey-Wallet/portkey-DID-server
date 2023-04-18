using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp;

namespace CAServer.Verifier;

public class VerificationSignatureRequestDto : IValidatableObject
{
    [Required] public string VerifierSessionId { get; set; }
    [Required] public string VerificationCode { get; set; }
    [Required] public string GuardianAccount { get; set; }

    [Required] public string VerifierId { get; set; }
    [Required] public string ChainId { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (VerifierSessionId.IsNullOrEmpty() || VerifierId.IsNullOrEmpty() ||
            VerificationCode.IsNullOrEmpty() || GuardianAccount.IsNullOrEmpty() || ChainId.IsNullOrEmpty())
        {
            yield return new ValidationResult("Input is null or empty");
        }
    }
}