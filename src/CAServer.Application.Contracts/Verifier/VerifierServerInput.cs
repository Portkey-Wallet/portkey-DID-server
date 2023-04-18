

using System.ComponentModel.DataAnnotations;

namespace CAServer.Verifier;

public class VerifierServerInput : VerifierServerBase
{
    [Required]
    public string ChainId { get; set; }

}