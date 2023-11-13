using System;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Common;
using Microsoft.AspNetCore.Http;
using Orleans.Runtime;
using Serilog.Context;

namespace CAServer;

public class RequestLogContextMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLogContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        var id = GetCorrelationId(context);
        
        using (LogContext.PushProperty("tracing_id", id))
        {
            RequestContext.Set("tracing_id", id);
            return _next.Invoke(context);
        }
    }

    public static string GetCorrelationId(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("tracing_id", out var correlationId))
        {
            DashExecutionContext.TrySetTraceIdentifier(correlationId);
        }
        else
        {
            DashExecutionContext.TrySetTraceIdentifier(httpContext.TraceIdentifier);
        }
        
        return correlationId.FirstOrDefault() ?? httpContext.TraceIdentifier;
    }
}