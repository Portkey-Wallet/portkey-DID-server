namespace CAServer.Telegram.Options;

public class JwtTokenOptions
{
    public string PublicKey { get; set; } 
    public string PrivateKey { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int expire { get; set; }
}