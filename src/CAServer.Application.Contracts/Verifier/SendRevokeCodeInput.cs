namespace CAServer.Verifier;

public class SendRevokeCodeInput
{
    public string GuardianIdentifier { get; set; }
    
    public string ChainId{ get; set; }

    public string Type { get; set; }

    public string VerifierId { get; set; }


}