using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CAServer.Nightingale.Http;

public class PerformanceMonitorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<PerformanceMonitorMiddlewareOptions> _optionsMonitor;
    private readonly ILogger<PerformanceMonitorMiddleware>? _logger;
    private readonly N9EClientFactory? _n9EClientFactory;

    public PerformanceMonitorMiddleware(IServiceProvider serviceProvider, RequestDelegate next,
        IOptionsMonitor<PerformanceMonitorMiddlewareOptions> optionsMonitor)
    {
        _next = next;
        _optionsMonitor = optionsMonitor;

        _logger = serviceProvider.GetService<ILogger<PerformanceMonitorMiddleware>>();
        _n9EClientFactory = serviceProvider.GetService<N9EClientFactory>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_optionsMonitor.CurrentValue.IsEnabled)
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
            await Track(context, stopwatch, false);
        }
        catch (Exception)
        {
            await Track(context, stopwatch, true);
            throw;
        }
    }

    private async Task Track(HttpContext context, Stopwatch stopwatch, bool isException)
    {
        if (_n9EClientFactory == null)
        {
            return;
        }

        try
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            var path = context.Request.Path;
            await _n9EClientFactory.TrackTransactionSync(chart: N9EClientConstant.Api, type: path, duration: elapsedMs,
                isSuccess: !isException);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "error recording http request");
        }
    }
}