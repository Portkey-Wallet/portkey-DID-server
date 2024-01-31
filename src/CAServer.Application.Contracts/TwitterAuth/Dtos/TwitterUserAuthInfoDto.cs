namespace CAServer.TwitterAuth.Dtos;

public class TwitterUserAuthInfoDto
{
    public TwitterUserInfo UserInfo { get; set; }
    public string AccessToken { get; set; }
    public string AuthType { get; set; } = "Twitter";
}