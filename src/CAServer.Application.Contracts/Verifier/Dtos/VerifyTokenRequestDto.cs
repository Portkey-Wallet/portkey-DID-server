using System.ComponentModel.DataAnnotations;
using CAServer.CAAccount.Dtos;

namespace CAServer.Verifier.Dtos;

public class VerifyTokenRequestDto
{
    [Required] public string AccessToken { get; set; }
    [Required] public string VerifierId { get; set; }
    [Required] public string ChainId { get; set; }

    public string TargetChainId { get; set; }
    [Required] public OperationType OperationType { get; set; }

    public string OperationDetails { get; set; }

    public string GuardianIdentifier { get; set; }
    
    public GuardianIdentifierType Type { get; set; }
    
    public string SecondaryEmail { get; set; }
    
    public string CaHash { get; set; }
}