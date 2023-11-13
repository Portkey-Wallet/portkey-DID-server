using System;
using System.Threading.Tasks;
using CAServer.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

namespace CAServer;

public class Program
{
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
            //.WriteTo.Async(c => c.Console())
            // .WriteTo.Console(outputTemplate:
            //   "[{Timestamp:HH:mm:ss fff} {Level:u3} {Properties:j}] - {Message}{NewLine}{Exception}")
            .WriteTo.Console(new ExpressionTemplate(
                "[{@t:HH:mm:ss fff} {@l:u3}" +
                " Application:{Application},Module:{Module}{#if tracing_id is not null},tracing_id:{tracing_id}{#end}{#if RequestPath is not null},RequestPath:{RequestPath}{#end}] {@m}\n{@x}"))
            // //.WriteTo.Console(new ExpressionTemplate("[{Timestamp:HH:mm:ss fff} {Level:u3}]-{tracing_id}{Message}{NewLine}{Exception}"))
#endif
            .CreateLogger();


        try
        {
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
                .WithMetrics(meterProviderBuilder =>
                {
                    meterProviderBuilder.AddAspNetCoreInstrumentation();
                    meterProviderBuilder.AddMeter("Microsoft.AspNetCore.Hosting");
                    meterProviderBuilder.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
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