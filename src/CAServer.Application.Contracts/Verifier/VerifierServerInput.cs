using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq.Dynamic.Core;
using CAServer.Commons;

namespace CAServer.Verifier;

public class VerifierServerInput : VerifierServerBase, IValidatableObject
{
    [Required] public string ChainId { get; set; }

    [Required] public OperationType OperationType { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type == "Email" && !VerifyHelper.VerifyEmail(GuardianIdentifier))
        {
            yield return new ValidationResult(
                "Invalid email input.",
                new[] { "GuardianIdentifier" }
            );
        }
    }
}