namespace CAServer.ThirdPart.Dtos.Ramp;

public class RampFreeLoginDto
{
    public string Email { get; set; }
    public string AccessToken { get; set; }
}

public class RampFreeLoginRequest
{
    public string ThirdPart { get; set; }
    public string Email { get; set; }
}