namespace CAServer;

public class RealIpOptions
{
    public string HeaderKey { get; set; }

    public bool AllowLegacyForwardedFallback { get; set; } = true;
}
