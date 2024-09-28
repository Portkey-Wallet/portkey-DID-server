namespace CAServer.IpInfo;

public class IpServiceSettingOptions
{
    public string BaseUrl { get; set; }
    public string AccessKey { get; set; }
    public string Language { get; set; }
    public int ExpirationDays { get; set; }
    public string HolderStatisticAccessKey { get; set; }
}