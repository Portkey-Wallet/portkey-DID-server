using System;

namespace CAServer.Verifier.Dtos;

public class VerifierCodeRequestDto
{
    public string Type { get; set; }
    public string GuardianIdentifier { get; set; }
    public Guid VerifierSessionId{ get; set; }
    public string VerifierId{ get; set; }
    
    public string ChainId { get; set; }
    public string TargetChainId { get; set; }
    
    public OperationType OperationType { get; set; }
    
    public string OperationDetails { get; set; }
    
    

}