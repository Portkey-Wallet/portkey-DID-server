using System.ComponentModel.DataAnnotations;

namespace CAServer.Signature.Dtos;

public class SignatureResponseDto
{
    public string Signature { get; set; }
}

public class SendSignatureDto
{
    [Required] public string PublicKey { get; set; }
    [Required] public string HexMsg { get; set; }
}