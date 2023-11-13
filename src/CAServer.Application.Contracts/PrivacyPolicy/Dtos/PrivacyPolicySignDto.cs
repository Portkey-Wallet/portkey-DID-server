using System.ComponentModel.DataAnnotations;

namespace CAServer.PrivacyPolicy.Dtos;

public class PrivacyPolicySignDto
{
    public int PolicyVersion { get; set; }
    [Required]
    public string CaHash { get; set; }
    [Required]
    public string Origin { get; set; }
    [Required]
    public int Scene { get; set; }
    [Required]
    public string ManagerAddress { get; set; }
    [Required]
    public string PolicyId { get; set; }
    public string Content { get; set; }
}