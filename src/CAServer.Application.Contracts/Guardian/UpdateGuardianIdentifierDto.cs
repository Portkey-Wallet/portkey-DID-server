using System;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Guardian;

public class UpdateGuardianIdentifierDto : GuardianIdentifierDto
{
    public Guid UserId { get; set; }
    
    [Required]
    public string UnsetGuardianIdentifierHash { get; set; }
}