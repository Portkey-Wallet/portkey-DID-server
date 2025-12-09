namespace CAServer.TwitterAuth.Dtos;

public class TwitterAuthDto
{
    public string State { get; set; }
    public string Code { get; set; }
    public string RedirectUrl { get; set; }
}