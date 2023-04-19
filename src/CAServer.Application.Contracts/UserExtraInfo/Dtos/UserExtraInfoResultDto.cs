using System;

namespace CAServer.UserExtraInfo.Dtos;

public class UserExtraInfoResultDto
{
    public string FullName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Picture { get; set; }
    public bool VerifiedEmail { get; set; }
    public bool IsPrivate { get; set; }
    public string GuardianType { get; set; }
    public DateTime AuthTime { get; set; }
}