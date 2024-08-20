using System.ComponentModel.DataAnnotations;

namespace CAServer.CAAccount.Dtos.Zklogin;

public class AppendSinglePoseidonDto
{
    [Required]
    public string CaHash { get; set; }
    [Required]
    public GuardianIdentifierType Type { get; set; }
    [Required]
    public string IdentifierHash { get; set; }
    [Required]
    public string PoseidonIdentifierHash { get; set; }
}