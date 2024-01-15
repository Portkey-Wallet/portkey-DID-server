namespace CAServer.Options;

public class SignatureServerOptions
{
    public string BaseUrl { get; set; }
    public string AppId { get; set; }
    public string AppSecret { get; set; }
    public int SecretCacheSeconds { get; set; } = 60;
}