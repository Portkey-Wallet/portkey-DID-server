using System.ComponentModel.DataAnnotations;

namespace CAServer.Verifier;

public class VerifierServerBase
{
    [Required]
    public string Type { get; set; }
    
    [Required]
    public string GuardianIdentifier{ get; set; }
    
    [Required]
    public string VerifierId { get; set; }
}