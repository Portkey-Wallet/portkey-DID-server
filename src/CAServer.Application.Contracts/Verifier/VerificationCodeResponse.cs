using System;
using System.Text.Json.Serialization;

namespace CAServer.Verifier;

public class VerificationCodeResponse
{
    public string VerificationDoc{ get; set; }

    public string Signature{ get; set; }
    
    public string Extra { get; set; }
    
    public string GuardianIdentifierHash { get; set; }
}