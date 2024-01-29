namespace CAServer.Verifier;

public class VierifierCodeRequestInput
{
    public string VerifierSessionId { get; set; }
    public string VerificationCode { get; set; }
    public string GuardianIdentifier { get; set; }
    public string VerifierId { get; set; }

    public string ChainId { get; set; }
    public string Salt { get; set; }
    public string GuardianIdentifierHash { get; set; }

    public OperationType OperationType { get; set; }

    public string TargetChainId { get; set; }

    public string OperationDetails { get; set; }
}