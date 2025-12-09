using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CAServer.Middleware;

public class DeviceInfoMiddleware
{
    
    private readonly ILogger<DeviceInfoMiddleware> _logger;
    private readonly RequestDelegate _next;

    public DeviceInfoMiddleware(RequestDelegate next, ILogger<DeviceInfoMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        DeviceInfoContext.CurrentDeviceInfo = ExtractDeviceInfo(context);
        try
        {
            await _next(context);
        }
        finally
        {
            DeviceInfoContext.Clear();
        }
    }

    private DeviceInfo ExtractDeviceInfo(HttpContext context)
    {
        try
        {
            var headers = context.Request.Headers;
            var clientTypeExists = headers.TryGetValue("Client-Type", out var clientType);
            var clientVersionExists = headers.TryGetValue("Version", out var clientVersion);

            return new DeviceInfo
            {
                ClientType = clientTypeExists ? clientType.ToString() : null,
                Version = clientVersionExists ? clientVersion.ToString() : null,
                ClientIp = GetClientIp(context)
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Decode device info error");
        }
        return null;
    }
    
    private string GetClientIp(HttpContext context)
    {
        // Check the X-Forwarded-For header (set by some agents)
        var forwardedHeader = context.Request.Headers["X-Forwarded-For"];
        if (!string.IsNullOrEmpty(forwardedHeader))
        {
            var ip = forwardedHeader.FirstOrDefault();
            if (!string.IsNullOrEmpty(ip))
            {
                return ip.Split(',')[0].Trim();// Take the first IP (if there are more than one)
            }
        }

        // Check the X-Real-IP header (set by some agents)
        var realIpHeader = context.Request.Headers["X-Real-IP"];
        if (!string.IsNullOrEmpty(realIpHeader))
        {
            return realIpHeader;
        }

        var ipAddress = context.Connection.RemoteIpAddress;

        // Use remote IP address as fallback
        return ipAddress?.IsIPv4MappedToIPv6 ?? false ? ipAddress.MapToIPv4().ToString() : ipAddress?.ToString();
    }
}