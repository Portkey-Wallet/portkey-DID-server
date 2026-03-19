namespace CAServer.IpInfo;

public interface IHttpClientIpResolver
{
    string GetForwardedClientIp();

    string GetBestEffortClientIp();

    string GetFirstHeaderIp(string headerName);

    void SetResolvedClientIp(string clientIp);
}

public static class ClientIpHeaders
{
    public const string XForwardedFor = "X-Forwarded-For";
    public const string XRealIp = "X-Real-IP";
}

public static class ClientIpContextItems
{
    public const string ResolvedClientIp = "CAServer:ResolvedClientIp";
}
