namespace CAServer.Facebook.Dtos;

public class FacebookAuthResponseDto
{
    public string Code { get; set; }

    public string Message { get; set; }

    public FacebookAuthResponse Data { get; set; }
}