namespace CAServer.Verifier;

public class VierifierCodeRequestInput
{
    public string VerifierSessionId { get; set; }
    public string VerificationCode { get; set; }
    public string GuardianAccount { get; set; }
    public string VerifierId{ get; set; }
    
    public string ChainId { get; set; }
}