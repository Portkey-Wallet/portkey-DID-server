using System;
using System.Threading.Tasks;
using CAServer.Hubs;
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
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("serilog.json")
            .Build();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("Starting CAServer.HttpApi.Host");
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("apollo.appsettings.json");
            builder.Configuration.AddJsonFile("appsettings.json");
            builder.Configuration.AddJsonFile("phone.json");
            builder.Configuration.AddJsonFile("ramp.json");
            builder.Configuration.AddJsonFile("seedurl.json");
            builder.Configuration.AddJsonFile("activity.json");

            var hostBuilder = builder.Host.AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog();
            
            if (builder.Configuration.GetSection("apollo").GetSection("UseApollo").Get<bool>())
            {
                hostBuilder.UseApollo();
            }

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