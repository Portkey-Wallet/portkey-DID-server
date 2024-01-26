namespace CAServer.TwitterAuth;

public class TwitterAuthOptions
{
    public string AccessTokenRedirectUrl { get; set; }
    public string RedirectUrl { get; set; }

    //https://api.twitter.com/2/oauth2/token
    public string TwitterTokenUrl { get; set; }

    public string ClientId { get; set; }
    public string ClientSecret { get; set; }

    public bool IsTest { get; set; } = true;
}