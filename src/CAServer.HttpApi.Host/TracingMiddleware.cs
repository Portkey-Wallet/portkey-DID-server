using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace CAServer;

public class TracingMiddleware
{
    
    private const string DashTraceIdentifier = "X-Dash-TraceIdentifier";
    private readonly RequestDelegate _next;

    public TracingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(DashTraceIdentifier, out var traceId))
        {
            
            
            httpContext.TraceIdentifier = traceId;
            //DashExecutionContext.TrySetTraceIdentifier(traceId);
        }
        else
        {
            Log.Debug($"Setting the detached HTTP Trace Identifier for {nameof(DashExecutionContext)}, because the HTTP context misses {DashTraceIdentifier} header!");
            //DashExecutionContext.TrySetTraceIdentifier(httpContext.TraceIdentifier);
        }
        
        httpContext.Response.OnStarting(state =>
        {
            var ctx = (HttpContext)state;
            ctx.Response.Headers.Add(DashTraceIdentifier, new[] { ctx.TraceIdentifier }); // there’s a reason not to use DashExecutionContext.TraceIdentifier value directly here

            return Task.CompletedTask;
        }, httpContext);

        await _next(httpContext);
    }
    
    
    
}