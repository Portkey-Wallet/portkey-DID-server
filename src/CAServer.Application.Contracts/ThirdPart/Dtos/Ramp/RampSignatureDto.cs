using System.ComponentModel.DataAnnotations;

namespace CAServer.ThirdPart.Dtos.Ramp;

public class RampSignatureDto
{
    public string Signature { get; set; }
}


public class RampSignatureRequest
{
    [Required] public string ThirdPart { get; set; }
    [Required] public string Address { get; set; }
}