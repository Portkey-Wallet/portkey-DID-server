using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using CAServer.Commons;

namespace CAServer.Dtos;

public class RecoveryRequestDto
{
    [Required] public string LoginGuardianAccount { get; set; }

    [ValidManagerAddress] [Required] public string ManagerAddress { get; set; }
    [Required] public List<RecoveryGuardian> GuardiansApproved { get; set; }
    [Required] public string DeviceString { get; set; }
    [Required] public string ChainId { get; set; }
    [Required] public HubRequestContextDto Context { get; set; }
}

public class RecoveryGuardian : IValidatableObject
{
    public GuardianTypeDto Type { get; set; }
    [Required] public string Value { get; set; }
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
        
        if (Type == GuardianTypeDto.Email && !VerifyHelper.VerifyEmail(Value))
        {
            yield return new ValidationResult(
                "Invalid email input.",
                new[] { "loginGuardianAccount" }
            );
        }

        if (Type == GuardianTypeDto.PhoneNumber && !VerifyHelper.VerifyPhone(Value))
        {
            yield return new ValidationResult(
                "Invalid phone number input.",
                new[] { "loginGuardianAccount" }
            );
        }
    }
}