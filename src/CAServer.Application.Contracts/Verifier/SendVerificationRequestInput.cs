using System;

namespace CAServer.Verifier;

public class SendVerificationRequestInput : VerifierServerBase
{
    public Guid VerifierSessionId { get; set; }

    public string ChainId { get; set; }
    
    public PlatformType PlatformType { get; set; }
    
    public OperationType OperationType { get; set; }
    
    public string OperationDetails { get; set; }

    public string TargetChainId { get; set; }
}