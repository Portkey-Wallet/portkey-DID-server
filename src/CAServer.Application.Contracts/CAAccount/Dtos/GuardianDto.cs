using CAServer.Account;

namespace CAServer.Dtos;

public class GuardianAccountInfoDto
{
    public string Value { get; set; }
    public GuardianType Type { get; set; }
    public VerificationInfoDto VerificationInfo { get; set; }
}

public class VerificationInfoDto
{
    public string Id { get; set; }
    public string VerificationDoc { get; set; }
    public string Signature { get; set; }
}