using Serilog;
using Serilog.Events;

namespace Orleans.TelemetryConsumers.Nightingale;

public static class LogHelper
{
    public static string FilePath { get; set; } =
        "./other/n9e-v6.7.3-linux-amd64/docker/compose-bridge/etc-categraf/logs/log-.log";
    public static ILogger CreateLogger(LogEventLevel logEventLevel)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Is(logEventLevel)
            .Enrich.FromLogContext()
            .WriteTo.Async(c =>
                c.Console(
                    outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}{Offset:zzz}][{Level:u3}] [{SourceContext}] {Message}{NewLine}"))
            .WriteTo.Async(c =>
                c.File(
                    FilePath,
                    rollingInterval: RollingInterval.Day,
                    outputTemplate:
                    "{Message}{NewLine}"))
            .CreateLogger();
    }
}