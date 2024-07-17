using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CAServer.CAAccount.Dtos;
using CAServer.Commons;

namespace CAServer.Dtos;

public class RecoveryRequestDto : IValidatableObject
{
    [Required] public string LoginGuardianIdentifier { get; set; }

    [ValidManager] [Required] public string Manager { get; set; }
    [Required] public List<RecoveryGuardian> GuardiansApproved { get; set; }
    [Required] public string ExtraData { get; set; }
    [Required] public string ChainId { get; set; }
    [Required] public HubRequestContextDto Context { get; set; }
    public ReferralInfo ReferralInfo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ReferralInfo is { ProjectCode: CommonConstant.CryptoGiftProjectCode } && ReferralInfo.ReferralCode.IsNullOrEmpty())
        {
            yield return new ValidationResult(
                "Invalid ReferralCode.",
                new[] { "LoginGuardianIdentifier" }
            );
        }
    }
}

public class RecoveryGuardian : IValidatableObject
{
    public GuardianIdentifierType Type { get; set; }
    [Required] public string Identifier { get; set; }
    [Required] public string VerifierId { get; set; }
    [Required] public string VerificationDoc { get; set; }
    [Required] public string Signature { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!Enum.IsDefined(Type))
        {
            yield return new ValidationResult(
                "Invalid type input.",
                new[] { "type" }
            );
        }

        if (Type == GuardianIdentifierType.Email && !VerifyHelper.VerifyEmail(Identifier))
        {
            yield return new ValidationResult(
                "Invalid email input.",
                new[] { "identifier" }
            );
        }

        // if (Type == GuardianIdentifierType.Phone && !VerifyHelper.VerifyPhone(Identifier))
        // {
        //     yield return new ValidationResult(
        //         "Invalid phone number input.",
        //         new[] { "identifier" }
        //     );
        // }
    }
}