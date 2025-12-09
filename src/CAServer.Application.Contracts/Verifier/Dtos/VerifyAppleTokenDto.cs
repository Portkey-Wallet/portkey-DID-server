using System;

namespace CAServer.Verifier.Dtos;

public class VerifyAppleTokenDto
{
    public string VerificationDoc{ get; set; }
    public string Signature{ get; set; }
    public AppleUserExtraInfo AppleUserExtraInfo { get; set; }
}

public class AppleUserExtraInfo
{
    public string Id { get; set; }
    public string Email { get; set; }
    public bool VerifiedEmail { get; set; }
    public bool IsPrivateEmail { get; set; }
    public string GuardianType { get; set; }
    public DateTime AuthTime { get; set; }
}