using System.ComponentModel.DataAnnotations;

namespace CAServer.Upgrade.Dtos;

public class UpgradeRequestDto
{
    [Required] public string Version { get; set; }
}