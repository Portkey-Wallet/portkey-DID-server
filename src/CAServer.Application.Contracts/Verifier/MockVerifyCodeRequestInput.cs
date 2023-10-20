using System.ComponentModel.DataAnnotations;

namespace CAServer.Verifier;

public class MockVerifyCodeRequestInput
{
    [Required] public string GuardianIdentifier { get; set; }
    [Required] public int Type { get; set; }

    [Required] public string VerifierId { get; set; }
    [Required] public string ChainId { get; set; }
    
    public string TargetChainId { get; set; }

    [Required] public OperationType OperationType { get; set; }
    
}