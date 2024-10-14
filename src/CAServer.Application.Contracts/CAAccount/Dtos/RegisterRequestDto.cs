using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AElf.Types;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Dtos.Zklogin;
using CAServer.Commons;
using Orleans;

namespace CAServer.Dtos;

public class RegisterRequestDto : IValidatableObject
{
    [Required] public GuardianIdentifierType Type { get; set; }
    [Required] public string LoginGuardianIdentifier { get; set; }

    [ValidManager] [Required] public string Manager { get; set; }

    [Required] public string ExtraData { get; set; }
    [Required] public string ChainId { get; set; }
    public string VerifierId { get; set; }
    public string VerificationDoc { get; set; }
    public string Signature { get; set; }
    [Required] public HubRequestContextDto Context { get; set; }
    public ReferralInfo ReferralInfo { get; set; }
    public ProjectDelegateInfo ProjectDelegateInfo { get; set; }
    
    public string AccessToken { get; set; }
    public ZkLoginInfoRequestDto ZkLoginInfo { get; set; }

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
        if (ReferralInfo is { ProjectCode: CommonConstant.CryptoGiftProjectCode } && ReferralInfo.ReferralCode.IsNullOrEmpty())
        {
            yield return new ValidationResult(
                "Invalid ReferralCode.",
                new[] { "ReferralInfo" }
            );
        }

        if (ZkLoginInfo != null)
        {
            if (ZkLoginInfo.ZkProof.IsNullOrEmpty())
            {
                yield return new ValidationResult(
                    "Invalid ZkLoginInfo ZkProof.",
                    new[] { "ZkProof" }
                );
            }

            if (ZkLoginInfo.Jwt.IsNullOrEmpty())
            {
                yield return new ValidationResult(
                    "Invalid ZkLoginInfo Jwt.",
                    new[] { "Jwt" }
                );
            }

            if (ZkLoginInfo.Salt.IsNullOrEmpty())
            {
                yield return new ValidationResult(
                    "Invalid ZkLoginInfo Salt.",
                    new[] { "Salt" }
                );
            }
            
            if (ZkLoginInfo.Nonce.IsNullOrEmpty())
            {
                yield return new ValidationResult(
                    "Invalid ZkLoginInfo Nonce.",
                    new[] { "Nonce" }
                );
            }
            
            if (ZkLoginInfo.CircuitId.IsNullOrEmpty())
            {
                yield return new ValidationResult(
                    "Invalid ZkLoginInfo CircuitId.",
                    new[] { "CircuitId" }
                );
            }
            
            if (ZkLoginInfo.PoseidonIdentifierHash.IsNullOrEmpty())
            {
                yield return new ValidationResult(
                    "Invalid ZkLoginInfo PoseidonIdentifierHash.",
                    new[] { "PoseidonIdentifierHash" }
                );
            }
                    
            if (ZkLoginInfo.Timestamp <= 0)
            {
                yield return new ValidationResult(
                    "Invalid ZkLoginInfo Timestamp.",
                    new[] { "Timestamp" }
                );
            }
        }
    }
}

[GenerateSerializer]
public class ProjectDelegateInfo
{
    [Id(0)]
    public int ChainId { get; set; }

    [Id(1)]
    public string ProjectHash { get; set; }

    [Id(2)]
    public string IdentifierHash { get; set; }

    [Id(3)]
    public int ExpirationTime { get; set; }

    [Id(4)]
    public Dictionary<string,long> Delegations { get; set; }

    [Id(5)]
    public bool IsUnlimitedDelegate { get; set; }

    [Id(6)]
    public string Signature { get; set; }

    [Id(7)]
    public long TimeStamp { get; set; }
    
}