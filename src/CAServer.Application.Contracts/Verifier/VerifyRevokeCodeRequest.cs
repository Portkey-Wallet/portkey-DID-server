using System;

namespace CAServer.Verifier;

public class VerifyRevokeCodeRequest
{
    
    public Guid VerifierSessionId { get; set; }
    public string VerifyCode { get; set; }

    public string ChainId { get; set; }
}