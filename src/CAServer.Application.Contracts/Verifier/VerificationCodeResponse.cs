using System;
using System.Text.Json.Serialization;

namespace CAServer.Verifier;

public class VerificationCodeResponse
{
    public string VerificationDoc{ get; set; }

    public string Signature{ get; set; }
}