using System;

namespace CAServer.Verifier;

public class SendRevokeCodeResponse
{
    public Guid VerifierSessionId { get; set; }
    
    public string VerifierId { get; set; }
}