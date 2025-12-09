using System.ComponentModel.DataAnnotations;

namespace CAServer.CAAccount.Cmd;

public class VerifySecondaryEmailCodeCmd
{
    [Required]
    public string VerifierSessionId { get; set; }
    
    [Required]
    public string VerificationCode { get; set; }
}