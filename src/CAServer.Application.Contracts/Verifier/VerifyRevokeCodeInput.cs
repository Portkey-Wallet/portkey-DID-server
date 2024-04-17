using System;

namespace CAServer.Verifier;

public class VerifyRevokeCodeInput
{
    public Guid VerifierSessionId { get; set; }

    public string VerifierId { get; set; }

    public string ChainId { get; set; }

    public string VerifyCode { get; set; }
    
    public string GuardianIdentifier { get; set; }

    public string Type { get; set; }

}