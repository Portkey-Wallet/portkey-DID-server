using System;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.IpInfo;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CAServer.Middleware;

public class DeviceInfoMiddleware
{
    private readonly IHttpClientIpResolver _clientIpResolver;
    private readonly ILogger<DeviceInfoMiddleware> _logger;
    private readonly RequestDelegate _next;

    public DeviceInfoMiddleware(RequestDelegate next, IHttpClientIpResolver clientIpResolver,
        ILogger<DeviceInfoMiddleware> logger)
    {
        _next = next;
        _clientIpResolver = clientIpResolver;
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
                ClientIp = _clientIpResolver.GetBestEffortClientIp()
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Decode device info error");
        }
        return null;
    }
}
