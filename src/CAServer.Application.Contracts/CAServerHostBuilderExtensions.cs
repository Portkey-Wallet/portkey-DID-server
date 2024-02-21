using Com.Ctrip.Framework.Apollo.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace CAServer;

public static class CAServerHostBuilderExtensions
{
    public static IHostBuilder InitAppConfiguration(this IHostBuilder hostBuilder, bool configureService)
    {
        return hostBuilder.AddAppSettingsApolloJson()
            .InitConfigurationHelper(configureService);
    }

    public static IHostBuilder UseApollo(this IHostBuilder hostBuilder)
    {
        if (!CAServer.Commons.ConfigurationHelper.IsApolloEnabled())
        {
            return hostBuilder.ConfigureGloballySharedLog(false);
        }

        //To display the Apollo console logs 
#if DEBUG
        LogManager.UseConsoleLogging(Com.Ctrip.Framework.Apollo.Logging.LogLevel.Trace);
#endif

        return hostBuilder
            .ConfigureAppConfiguration((_, builder) => { builder.AddApollo(builder.Build().GetSection("apollo")); })
            .ConfigureGloballySharedLog(false);
    }

    private static IHostBuilder AddAppSettingsApolloJson(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureAppConfiguration(
            (_, builder) => { builder.AddJsonFile("appsettings.apollo.json"); });
    }

    private static IHostBuilder InitConfigurationHelper(this IHostBuilder hostBuilder, bool configureService)
    {
        return configureService
            ? hostBuilder.ConfigureServices((context, _) =>
            {
                CAServer.Commons.ConfigurationHelper.Initialize(context.Configuration);
            })
            : hostBuilder.ConfigureAppConfiguration((context, _) =>
            {
                CAServer.Commons.ConfigurationHelper.Initialize(context.Configuration);
            });
    }

    private static IHostBuilder ConfigureGloballySharedLog(this IHostBuilder hostBuilder, bool configureService)
    {
        return configureService
            ? hostBuilder.ConfigureServices((context, _) =>
            {
                ConfigureGloballySharedLog(context.Configuration);
                SetGloballySharedLoggerForApolloLogs();
            })
            : hostBuilder.ConfigureAppConfiguration((context, _) =>
            {
                ConfigureGloballySharedLog(context.Configuration);
                SetGloballySharedLoggerForApolloLogs();
            });
    }

    private static void ConfigureGloballySharedLog(IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(configuration)
#if DEBUG
            .WriteTo.Async(c => c.Console())
#endif
            .CreateLogger();
    }

    private static void SetGloballySharedLoggerForApolloLogs()
    {
        LogManager.LogFactory = name => (level, message, exception) =>
        {
            var messageTemplate = $"{name} {message}";
            switch (level)
            {
                case LogLevel.Information:
                    Log.Information(messageTemplate);
                    break;
                case LogLevel.Warning:
                    Log.Warning(messageTemplate);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    if (exception == null)
                    {
                        Log.Error(messageTemplate);
                    }
                    else
                    {
                        Log.Error(exception, messageTemplate);
                    }

                    break;
                case LogLevel.Trace:
                case LogLevel.Debug:
                default:
                    Log.Debug(messageTemplate);
                    break;
            }
        };
    }

    public static IHostBuilder UseApolloForSilo(this IHostBuilder hostBuilder)
    {
        hostBuilder = hostBuilder
            .ConfigureAppConfiguration((_, builder) =>
            {
                if (!builder.Build().GetValue<bool>("IsApolloEnabled", false))
                {
                    return;
                }
                //To display the Apollo console logs 
#if DEBUG
                LogManager.UseConsoleLogging(Com.Ctrip.Framework.Apollo.Logging.LogLevel.Trace);
#endif
                builder.AddApollo(builder.Build().GetSection("apollo"));
            })
            .ConfigureGloballySharedLog(true);
        return hostBuilder;
    }
}