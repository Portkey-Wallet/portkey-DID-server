namespace CAServer.Account;

public class CAGuardian
{
    public int Type { get; set; }
    public string GuardianType { get; set; }
    public CAVerifier Verifier { get; set; }
}