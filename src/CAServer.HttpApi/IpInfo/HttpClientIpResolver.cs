using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.IpInfo;

public class HttpClientIpResolver : IHttpClientIpResolver, ITransientDependency
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly RealIpOptions _realIpOptions;

    public HttpClientIpResolver(IHttpContextAccessor httpContextAccessor, IOptions<RealIpOptions> realIpOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _realIpOptions = realIpOptions.Value ?? new RealIpOptions();
    }

    public string GetForwardedClientIp()
    {
        return GetHeaderIpInOrder(BuildForwardedHeaderCandidates());
    }

    public string GetBestEffortClientIp()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return null;
        }

        if (context.Items.TryGetValue(ClientIpContextItems.ResolvedClientIp, out var resolvedClientIp) &&
            resolvedClientIp is string requestScopedClientIp &&
            !string.IsNullOrWhiteSpace(requestScopedClientIp))
        {
            return requestScopedClientIp;
        }

        return GetHeaderIpInOrder(BuildBestEffortHeaderCandidates()) ??
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

        context.Items[ClientIpContextItems.ResolvedClientIp] = clientIp;
    }

    private IEnumerable<string> BuildForwardedHeaderCandidates()
    {
        yield return NormalizeHeaderName(_realIpOptions.HeaderKey);

        if (!_realIpOptions.AllowLegacyForwardedFallback)
        {
            yield break;
        }

        yield return ClientIpHeaders.XForwardedFor;
        yield return ClientIpHeaders.XRealIp;
    }

    private IEnumerable<string> BuildBestEffortHeaderCandidates()
    {
        yield return NormalizeHeaderName(_realIpOptions.HeaderKey);

        if (!_realIpOptions.AllowLegacyForwardedFallback)
        {
            yield break;
        }

        yield return ClientIpHeaders.XForwardedFor;
        yield return ClientIpHeaders.XRealIp;
    }

    private string GetHeaderIpInOrder(IEnumerable<string> headerNames)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var headerName in headerNames)
        {
            if (string.IsNullOrWhiteSpace(headerName) || !visited.Add(headerName))
            {
                continue;
            }

            var headerIp = GetFirstHeaderIp(headerName);
            if (!string.IsNullOrWhiteSpace(headerIp))
            {
                return headerIp;
            }
        }

        return null;
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

    private static string NormalizeHeaderName(string headerName)
    {
        return string.IsNullOrWhiteSpace(headerName) ? null : headerName.Trim();
    }
}
