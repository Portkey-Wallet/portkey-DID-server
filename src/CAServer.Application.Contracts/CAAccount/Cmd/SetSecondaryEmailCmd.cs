using System.ComponentModel.DataAnnotations;

namespace CAServer.CAAccount.Cmd;

public class SetSecondaryEmailCmd
{
    [Required]
    public string VerifierSessionId { get; set; }
}