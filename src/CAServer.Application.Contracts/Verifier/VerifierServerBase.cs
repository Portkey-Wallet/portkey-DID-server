using System;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Verifier;

public class VerifierServerBase
{
    [Required]
    public string Type { get; set; }
    
    [Required]
    public string GuardianAccount{ get; set; }
    
    [Required]
    public string VerifierId { get; set; }    
    

    
}