using System;
using System.Threading.Tasks;
using CAServer.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using System.Diagnostics.Metrics;
using Meter = System.Diagnostics.Metrics.Meter;

namespace CAServer;

public class Program
{
    static Meter s_meter = new Meter("HatCo.Store");
    static Counter<int> s_hatsSold = s_meter.CreateCounter<int>("hatco.store.hats_sold");
    
    public async static Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(configuration)
            
#if DEBUG
            .WriteTo.Async(c => c.Console())
#endif          
            .CreateLogger();

        try
        {
            s_hatsSold.Add(4);
            Log.Information("Starting CAServer.HttpApi.Host.");
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("phone.json");
            builder.Configuration.AddJsonFile("seedurl.json");
            builder.Host.AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog();
            builder.Services.AddSignalR();
            builder.Services.AddRazorPages();
            builder.Services.AddOpenTelemetry()
                .WithTracing(builder =>
                {
                    builder.AddAspNetCoreInstrumentation();
                    builder.AddConsoleExporter();
                });
            await builder.AddApplicationAsync<CAServerHttpApiHostModule>();
            var app = builder.Build();
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            //app.MapRazorPages();
            
            app.MapHub<CAHub>("ca");
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
