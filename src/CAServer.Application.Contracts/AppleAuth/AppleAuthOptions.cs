using System.Collections.Generic;

namespace CAServer.AppleAuth;

public class AppleAuthOptions
{
    public List<string> Audiences { get; set; }
    public string RedirectUrl { get; set; }
    public ExtensionConfig ExtensionConfig { get; set; }
}

public class ExtensionConfig
{
    public string PrivateKey { get; set; }
    public string TeamId { get; set; }
    public string ClientId { get; set; }
    public string KeyId { get; set; }
}