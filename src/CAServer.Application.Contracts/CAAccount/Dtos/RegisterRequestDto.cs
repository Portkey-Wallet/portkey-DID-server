using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AElf.Types;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using CAServer.Commons;

namespace CAServer.Dtos;

public class RegisterRequestDto : IValidatableObject
{
    [Required] public GuardianIdentifierType Type { get; set; }
    [Required] public string LoginGuardianIdentifier { get; set; }

    [ValidManager] [Required] public string Manager { get; set; }

    [Required] public string ExtraData { get; set; }
    [Required] public string ChainId { get; set; }
    [Required] public string VerifierId { get; set; }
    [Required] public string VerificationDoc { get; set; }
    [Required] public string Signature { get; set; }
    [Required] public HubRequestContextDto Context { get; set; }
    public ProjectDelegateInfo ProjectDelegateInfo { get; set; }

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
        
        if (Type == GuardianIdentifierType.Email && !VerifyHelper.VerifyEmail(LoginGuardianIdentifier))
        {
            yield return new ValidationResult(
                "Invalid email input.",
                new[] { "LoginGuardianIdentifier" }
            );
        }

        // if (Type == GuardianIdentifierType.Phone && !VerifyHelper.VerifyPhone(LoginGuardianIdentifier))
        // {
        //     yield return new ValidationResult(
        //         "Invalid phone number input.",
        //         new[] { "LoginGuardianIdentifier" }
        //     );
        // }
    }
}

public class ProjectDelegateInfo
{
    public int ChainId { get; set; }
    public string ProjectHash { get; set; }
    public string IdentifierHash { get; set; }
    public int ExpirationTime { get; set; }
    public Dictionary<string,long> Delegations { get; set; }
    public bool IsUnlimitedDelegate { get; set; }
    public string Signature { get; set; }
    
}