namespace CAServer.TwitterAuth;

public class TwitterAuthOptions
{
    public string RequestRedirectUrl { get; set; }

    //https://api.twitter.com/2/oauth2/token
    public string TwitterTokenUrl { get; set; }

    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string RedirectUrl { get; set; }
    public string UnifyRedirectUrl { get; set; }
}