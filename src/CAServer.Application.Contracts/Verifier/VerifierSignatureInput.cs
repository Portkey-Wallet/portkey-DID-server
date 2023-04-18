using System.ComponentModel.DataAnnotations;

namespace CAServer.Dtos
{
    public class VerifierSignatureInput
    {
        [Range(0, 1)] public int Type { get; set; } = -1;
        [Required] public string LoginGuardianType { get; set; }
        [Required] public string ManagerUniqueId { get; set; }
        [Required] public string VerifierName { get; set; }
        [Required] public string VerificationDoc { get; set; }
        [Required] public string VerifierSignature { get; set; }
    }
}