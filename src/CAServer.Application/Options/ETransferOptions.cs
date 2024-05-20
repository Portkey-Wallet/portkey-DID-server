namespace CAServer.Options;

public class ETransferOptions
{
    public string AuthBaseUrl { get; set; }
    public string AuthPrefix { get; set; }
    public string BaseUrl { get; set; }
    public string Prefix { get; set; }
    public int Timeout { get; set; } = 20;
}