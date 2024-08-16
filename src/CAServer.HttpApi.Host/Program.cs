using System;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Hubs;
using CAServer.Nightingale;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
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
                .UseSerilog();

            await builder.AddApplicationAsync<CAServerHttpApiHostModule>();
            builder.Services.AddRateLimiter(o => o
                .AddFixedWindowLimiter(policyName: "fixed", options =>
                {
                    options.PermitLimit = 1;
                    options.Window = TimeSpan.FromSeconds(10);
                    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    options.QueueLimit = 0;
                }));
            var app = builder.Build();
            app.MapHub<CAHub>("ca");
            //app.MapHub<DataReportingHub>("dataReporting");
            app.UseRateLimiter();
            // app.MapPost("/api/app/telegramAuth/bot/register",
            //     () => Results.Ok("You have visited too many times")).RequireRateLimiting("fixed");
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