namespace CAServer.ThirdPart.Dtos.Ramp;

public class RampSignatureDto
{
    public string Signature { get; set; }
}


public class RampSignatureRequest
{
    public string ThirdPart { get; set; }
    public string Address { get; set; }
}