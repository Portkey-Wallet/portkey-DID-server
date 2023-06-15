using System.ComponentModel.DataAnnotations;

namespace CAServer.AppleMigrate;

public class AppleMigrateRequestDto
{
    [Required]
    public string GuardianIdentifier { get; set; }
    
    [Required]
    public string MigratedUserId { get; set; }
}