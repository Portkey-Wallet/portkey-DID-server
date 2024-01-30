using System;

namespace CAServer.Verifier.Dtos;

public class VerifyTwitterTokenDto
{
    public string VerificationDoc { get; set; }
    public string Signature { get; set; }
    public TwitterUserExtraInfo TwitterUserExtraInfo { get; set; }
}

public class TwitterUserExtraInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string UserName { get; set; }
    public bool Verified { get; set; }
    public string GuardianType { get; set; }
    public DateTime AuthTime { get; set; }
}