using System;
using System.Threading.Tasks;
using CAServer.AuthServer;
using Com.Ctrip.Framework.Apollo.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using ConfigurationHelper = CAServer.Commons.ConfigurationHelper;

namespace CAServer;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        //To display the Apollo console logs 
#if DEBUG
        LogManager.UseConsoleLogging(Com.Ctrip.Framework.Apollo.Logging.LogLevel.Trace);
#endif

        try
        {
            Log.Information("Starting CAServer.AuthServer.");
            var builder = WebApplication.CreateBuilder(args);
            ConfigurationHelper.Initialize(builder.Configuration);
            builder.Configuration.AddJsonFile("apollosettings.json");
            builder.Host.AddAppSettingsSecretsJson();

            if (ConfigurationHelper.IsApolloEnabled())
            {
                builder.Host.UseApollo();
            }

            builder.Host.UseAutofac().UseSerilog();
            
            ConfigureGloballySharedLog(builder.Configuration);

            await builder.AddApplicationAsync<CAServerAuthServerModule>();
            var app = builder.Build();
            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0; 
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "CAServer.AuthServer terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
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

        SetGloballySharedLoggerForApolloLogs();
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
}