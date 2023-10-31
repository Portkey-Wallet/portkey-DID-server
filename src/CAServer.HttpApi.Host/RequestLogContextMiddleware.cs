using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
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
        using (LogContext.PushProperty("CorrelationId", id))
        {
            return _next.Invoke(context);
        }
    }

    public static string GetCorrelationId(HttpContext httpContext)
    {
        httpContext.Request.Headers.TryGetValue("Cko-Correlation-Id", out var correlationId);
        return correlationId.FirstOrDefault() ?? "I am tracing ID";
    }
}