using System.ComponentModel.DataAnnotations;

namespace CAServer.CAAccount.Dtos;

public class RevokeDto
{
    [Required] public string AppleToken { get; set; }
}