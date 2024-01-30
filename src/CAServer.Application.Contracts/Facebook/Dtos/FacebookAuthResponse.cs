namespace CAServer.Facebook.Dtos;

public class FacebookAuthResponse
{
    public string AccessToken { get; set; }
    
    public string UserId { get; set; }
    
    public long ExpiresTime { get; set; }
    
}