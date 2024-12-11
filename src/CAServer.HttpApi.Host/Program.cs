using System;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Hubs;
using CAServer.Nightingale;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace CAServer;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = LogHelper.CreateLogger(LogEventLevel.Debug);

        try
        {
            Log.Information("Starting CAServer.HttpApi.Host");
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("phone.json");
            builder.Configuration.AddJsonFile("ramp.json");
            builder.Configuration.AddJsonFile("seedurl.json");
            builder.Configuration.AddJsonFile("activity.json");
            builder.Configuration.AddJsonFile("userToken.json");

            var hostBuilder = builder.Host.AddAppSettingsSecretsJson()
                .InitAppConfiguration(false)
                .UseApolloForConfigureHostBuilder()
                .UseNightingaleMonitoring()
                .UseAutofac()
                .UseOrleansClient()
                .UseSerilog();

            await builder.AddApplicationAsync<CAServerHttpApiHostModule>();
            var app = builder.Build();
            app.MapHub<CAHub>("ca");
            //app.MapHub<DataReportingHub>("dataReporting");
            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}