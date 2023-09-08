using System.ComponentModel.DataAnnotations;

namespace CAServer.Verifier.Dtos;

public class VerifyTokenRequestDto
{
    [Required] public string AccessToken { get; set; }
    [Required] public string VerifierId { get; set; }
    [Required] public string ChainId { get; set; }
    
    [Required] public OperationType OperationType { get; set; }
    
    public string MerklePath { get; set; }
}