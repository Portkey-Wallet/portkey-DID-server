namespace CAServer.Account;

public class GuardianAccountInfo
{
    public string Value { get; set; }
    public GuardianType Type { get; set; }
    public VerificationInfo VerificationInfo { get; set; }
}

public class VerificationInfo
{
    public string Id { get; set; }
    public string VerificationDoc { get; set; }
    public string Signature { get; set; }
}