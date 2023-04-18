namespace CAServer.Account;

public class GuardianAccount
{
    public Guardian Guardian { get; set; }
    public string Value { get; set; }
}

public class Guardian
{
    public GuardianType Type { get; set; }
    public Verifier Verifier { get; set; }
}

public class Verifier
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string VerificationDoc { get; set; }
    public string Signature { get; set; }
}