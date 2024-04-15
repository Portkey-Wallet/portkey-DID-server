using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

namespace CAServer.Nightingale.Logging;

public static class N9EClientLogHelper
{
    public static ILogger CreateLogger(IOptionsMonitor<N9EClientForLoggingOptions> options)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Debug)
            .Enrich.FromLogContext()
#if DEBUG
            .WriteTo.Async(c =>
                c.Console(
                    outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}{Offset:zzz}][{Level:u3}] {Message}{NewLine}"))
#endif
            .WriteTo.Async(c =>
                c.File(
                    options.CurrentValue.LogFilePathFormat,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: options.CurrentValue.LogRetainedFileCountLimit,
                    outputTemplate:
                    "{Message}{NewLine}"))
            .CreateLogger();
    }
}