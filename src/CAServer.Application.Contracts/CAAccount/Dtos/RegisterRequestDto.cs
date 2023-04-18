using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using CAServer.Commons;

namespace CAServer.Dtos;

public class RegisterRequestDto : IValidatableObject
{
    [Required] public GuardianTypeDto Type { get; set; }
    [Required] public string LoginGuardianAccount { get; set; }

    [ValidManagerAddress] [Required] public string ManagerAddress { get; set; }

    [Required] public string DeviceString { get; set; }
    [Required] public string ChainId { get; set; }
    [Required] public string VerifierId { get; set; }
    [Required] public string VerificationDoc { get; set; }
    [Required] public string Signature { get; set; }
    [Required] public HubRequestContextDto Context { get; set; }

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
        
        if (Type == GuardianTypeDto.Email && !VerifyHelper.VerifyEmail(LoginGuardianAccount))
        {
            yield return new ValidationResult(
                "Invalid email input.",
                new[] { "LoginGuardianAccount" }
            );
        }

        if (Type == GuardianTypeDto.PhoneNumber && !VerifyHelper.VerifyPhone(LoginGuardianAccount))
        {
            yield return new ValidationResult(
                "Invalid phone number input.",
                new[] { "LoginGuardianAccount" }
            );
        }
    }
}