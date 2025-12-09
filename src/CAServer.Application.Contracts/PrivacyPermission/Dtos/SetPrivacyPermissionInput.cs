using System.ComponentModel.DataAnnotations;

namespace CAServer.PrivacyPermission.Dtos;

public class SetPrivacyPermissionInput
{
    [Required]
    public string Identifier { get; set; }
    [Required]
    public PrivacySetting Permission { get; set; }
    [Required]
    public PrivacyType PrivacyType { get; set; }
}