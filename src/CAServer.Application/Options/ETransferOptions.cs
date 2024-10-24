namespace CAServer.Options;

public class ETransferOptions
{
    public string AuthBaseUrl { get; set; }
    public string AuthPrefix { get; set; } = string.Empty;
    public string BaseUrl { get; set; }
    public string Prefix { get; set; } = string.Empty;
    public int Timeout { get; set; } = 20;
    public string Version { get; set; }
    public string EBridgeLimiterUrl { get; set; }
}