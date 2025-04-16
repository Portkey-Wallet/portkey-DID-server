using System.Collections.Generic;

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
    public List<SendNetwork> NotAvailableSendNetworks { get; set; } = new();
}

public class SendNetwork
{
    public string FromNetwork { get; set; }
    public string ToNetwork { get; set; }
    public string Symbol { get; set; }
}
