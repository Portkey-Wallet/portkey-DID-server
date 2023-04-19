using System;

namespace CAServer.Verifier.Dtos;

public class VerifyGoogleTokenDto
{
    public string VerificationDoc{ get; set; }
    public string Signature{ get; set; }
    public GoogleUserExtraInfo GoogleUserExtraInfo { get; set; }
}

public class GoogleUserExtraInfo
{
    public string Id { get; set; }
    public string FullName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Picture { get; set; }
    public bool VerifiedEmail { get; set; }
    public string GuardianType { get; set; }
    public DateTime AuthTime { get; set; }
}

public class HashInfo
{
    public string Salt { get; set; }
    public string IdentifierHash { get; set; }
}