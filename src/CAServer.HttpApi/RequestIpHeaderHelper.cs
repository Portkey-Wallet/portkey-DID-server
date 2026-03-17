using System;
using Microsoft.AspNetCore.Http;

namespace CAServer;

public static class RequestIpHeaderHelper
{
    public const string XForwardedFor = "X-Forwarded-For";
    public const string XRealIp = "X-Real-IP";

    public static string GetForwardedClientIp(HttpRequest request)
    {
        return GetFirstHeaderIp(request, XForwardedFor) ?? GetFirstHeaderIp(request, XRealIp);
    }

    private static string GetFirstHeaderIp(HttpRequest request, string headerName)
    {
        if (!request.Headers.TryGetValue(headerName, out var headerValue))
        {
            return null;
        }

        var ips = headerValue.ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var ip in ips)
        {
            if (!string.IsNullOrWhiteSpace(ip))
            {
                return ip;
            }
        }

        return null;
    }
}
