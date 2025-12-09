namespace CAServer.TwitterAuth.Dtos;

public class TwitterAuthResultDto
{
    public TwitterUserAuthInfoDto Data { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }
}