using System;
using CAServer.CAAccount.Dtos;
using CAServer.Verifier;

namespace CAServer.CAAccount;

public class RevokeAccountInput
{
    public string Token { get; set; }
    public string GuardianIdentifier { get; set; }
    public Guid VerifierSessionId { get; set; }
    public string VerifierId { get; set; }
    public string ChainId { get; set; }
    public string Type { get; set; }
    public VerificationRequestInfo VerificationRequestInfo { get; set; }
}