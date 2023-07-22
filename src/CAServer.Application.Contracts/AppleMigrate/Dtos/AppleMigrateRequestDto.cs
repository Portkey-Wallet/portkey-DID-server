using System.ComponentModel.DataAnnotations;

namespace CAServer.AppleMigrate.Dtos;

public class AppleMigrateRequestDto
{
    [Required]
    public string GuardianIdentifier { get; set; }

    [Required]
    public string MigratedUserId { get; set; }
}