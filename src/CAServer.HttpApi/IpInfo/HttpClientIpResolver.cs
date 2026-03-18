using System;
using Microsoft.AspNetCore.Http;
using Volo.Abp.DependencyInjection;

namespace CAServer.IpInfo;

public class HttpClientIpResolver : IHttpClientIpResolver, ITransientDependency
{
    private const string ResolvedClientIpItemKey = "CAServer:ResolvedClientIp";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpClientIpResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetForwardedClientIp()
    {
        return GetFirstHeaderIp(ClientIpHeaders.XForwardedFor) ?? GetFirstHeaderIp(ClientIpHeaders.XRealIp);
    }

    public string GetBestEffortClientIp()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return null;
        }

        if (context.Items.TryGetValue(ResolvedClientIpItemKey, out var resolvedClientIp) &&
            resolvedClientIp is string requestScopedClientIp &&
            !string.IsNullOrWhiteSpace(requestScopedClientIp))
        {
            return requestScopedClientIp;
        }

        return GetFirstHeaderIp(ClientIpHeaders.XForwardedFor) ??
               GetFirstHeaderIp(ClientIpHeaders.XRealIp) ??
               GetRemoteIp(context);
    }

    public string GetFirstHeaderIp(string headerName)
    {
        if (string.IsNullOrWhiteSpace(headerName))
        {
            return null;
        }

        var context = _httpContextAccessor.HttpContext;
        if (context == null || !context.Request.Headers.TryGetValue(headerName, out var headerValue))
        {
            return null;
        }

        foreach (var ip in headerValue.ToString()
                     .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(ip))
            {
                return ip;
            }
        }

        return null;
    }

    public void SetResolvedClientIp(string clientIp)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null || string.IsNullOrWhiteSpace(clientIp))
        {
            return;
        }

        context.Items[ResolvedClientIpItemKey] = clientIp;
    }

    private static string GetRemoteIp(HttpContext context)
    {
        var remoteIpAddress = context.Connection.RemoteIpAddress;
        if (remoteIpAddress == null)
        {
            return null;
        }

        return remoteIpAddress.IsIPv4MappedToIPv6
            ? remoteIpAddress.MapToIPv4().ToString()
            : remoteIpAddress.ToString();
    }
}
