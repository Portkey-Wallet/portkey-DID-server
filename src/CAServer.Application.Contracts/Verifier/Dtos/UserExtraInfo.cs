using System;

namespace CAServer.Verifier.Dtos;

public class UserExtraInfo
{
    public string Id { get; set; }
    public string FullName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Picture { get; set; }
    public bool VerifiedEmail { get; set; }
    public bool IsPrivateEmail { get; set; }
    public string GuardianType { get; set; }
    public DateTime AuthTime { get; set; }
}