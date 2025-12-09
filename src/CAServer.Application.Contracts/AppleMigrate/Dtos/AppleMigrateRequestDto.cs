using System.ComponentModel.DataAnnotations;

namespace CAServer.AppleMigrate.Dtos;

public class AppleMigrateRequestDto
{
    [Required]
    public string GuardianIdentifier { get; set; }

    [Required]
    public string MigratedUserId { get; set; }
    
    public string Email { get; set; }
    public bool IsPrivateEmail { get; set; }
}