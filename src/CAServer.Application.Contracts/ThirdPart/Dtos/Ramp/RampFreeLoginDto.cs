using System.ComponentModel.DataAnnotations;

namespace CAServer.ThirdPart.Dtos.Ramp;

public class RampFreeLoginDto
{
    public string Email { get; set; }
    public string AccessToken { get; set; }
}

public class RampFreeLoginRequest
{
    [Required] public string ThirdPart { get; set; }
    [Required] public string Email { get; set; }
}