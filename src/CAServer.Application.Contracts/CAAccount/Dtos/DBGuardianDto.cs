using System.ComponentModel.DataAnnotations;

namespace CAServer.CAAccount.Dtos;

public class DBGuardianDto
{
    [Required]
    public string GuardianIdentifier { get; set; }
    
    [Required]
    public string Salt { get; set; }
    
    [Required]
    public string IdentifierHash { get; set; }
}