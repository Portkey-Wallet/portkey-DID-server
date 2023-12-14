using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

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

    public async Task Invoke(HttpContext context)
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
            if (headers.IsNullOrEmpty()) return null;

            var clientTypeExists = headers.TryGetValue("ClientType", out var clientType);
            var clientVersionExists = headers.TryGetValue("Version", out var clientVersion);
            if (!clientTypeExists && !clientVersionExists) return null;

            return new DeviceInfo
            {
                ClientType = clientType.ToString().ToUpper(),
                Version = clientVersion.ToString().ToUpper()
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Decode device info error");
        }
        return null;
    }
}