using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CAServer.CAAccount;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Dtos.Zklogin;
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
    
    public RequestSource Source { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ReferralInfo is { ProjectCode: CommonConstant.CryptoGiftProjectCode } && ReferralInfo.ReferralCode.IsNullOrEmpty())
        {
            yield return new ValidationResult(
                "Invalid ReferralCode.",
                new[] { "LoginGuardianIdentifier" }
            );
        }

        if (!GuardiansApproved.IsNullOrEmpty())
        {
            foreach (var recoveryGuardian in GuardiansApproved)
            {
                if (recoveryGuardian.ZkLoginInfo != null)
                {
                    if (recoveryGuardian.ZkLoginInfo.ZkProof.IsNullOrEmpty())
                    {
                        yield return new ValidationResult(
                            "Invalid ZkLoginInfo ZkProof.",
                            new[] { "ZkProof" }
                        );
                    }

                    if (recoveryGuardian.ZkLoginInfo.Jwt.IsNullOrEmpty())
                    {
                        yield return new ValidationResult(
                            "Invalid ZkLoginInfo Jwt.",
                            new[] { "Jwt" }
                        );
                    }

                    if (recoveryGuardian.ZkLoginInfo.Salt.IsNullOrEmpty())
                    {
                        yield return new ValidationResult(
                            "Invalid ZkLoginInfo Salt.",
                            new[] { "Salt" }
                        );
                    }
            
                    if (recoveryGuardian.ZkLoginInfo.Nonce.IsNullOrEmpty())
                    {
                        yield return new ValidationResult(
                            "Invalid ZkLoginInfo Nonce.",
                            new[] { "Nonce" }
                        );
                    }
            
                    if (recoveryGuardian.ZkLoginInfo.CircuitId.IsNullOrEmpty())
                    {
                        yield return new ValidationResult(
                            "Invalid ZkLoginInfo CircuitId.",
                            new[] { "CircuitId" }
                        );
                    }

                    if (recoveryGuardian.ZkLoginInfo.PoseidonIdentifierHash.IsNullOrEmpty())
                    {
                        yield return new ValidationResult(
                            "Invalid ZkLoginInfo PoseidonIdentifierHash.",
                            new[] { "PoseidonIdentifierHash" }
                        );
                    }
                    
                    if (recoveryGuardian.ZkLoginInfo.Timestamp <= 0)
                    {
                        yield return new ValidationResult(
                            "Invalid ZkLoginInfo Timestamp.",
                            new[] { "Timestamp" }
                        );
                    }
                }
            }
        }
    }
}

public class RecoveryGuardian : IValidatableObject
{
    public GuardianIdentifierType Type { get; set; }
    [Required] public string Identifier { get; set; }
    public string VerifierId { get; set; }
    public string VerificationDoc { get; set; }
    public string Signature { get; set; }
    public ZkLoginInfoRequestDto ZkLoginInfo { get; set; }
    
    public VerificationRequestInfo VerificationRequestInfo { get; set; }

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