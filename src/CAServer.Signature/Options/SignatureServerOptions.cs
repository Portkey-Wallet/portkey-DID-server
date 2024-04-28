namespace CAServer.Signature.Options;

public class SignatureServerOptions
{
    public string BaseUrl { get; set; }
    public string AppId { get; set; }
    public string AppSecret { get; set; }
    public int SecretCacheSeconds { get; set; } = 60;
    public KeyIds KeyIds { get; set; } = new();
}



public class KeyIds
{
    public string CoinGecko { get; set; } = "CoinGeckoCaServer";
    public string IpService { get; set; } = "IpServiceCaServer";
    public string AwsS3IdentityPool { get; set; } = "AwsS3IdentityPoolIm";
    public string GoogleRecaptcha { get; set; } = "GoogleRecaptchaCaServer";
    public string FacebookAppSecret { get; set; } = "FacebookAppSecretCaServer";

}