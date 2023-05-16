using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CAServer.AppleAuth;
using CAServer.AppleAuth.Dtos;
using CAServer.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Volo.Abp;

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
            .WriteTo.Async(c => c.Console())
#endif
            .CreateLogger();

        try
        {
            Log.Information("Starting CAServer.HttpApi.Host.");
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("phone.json");
            builder.Host.AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog();
            builder.Services.AddSignalR();
            await builder.AddApplicationAsync<CAServerHttpApiHostModule>();
            var app = builder.Build();

            app.Use(async (context, next) =>
            {
                if (context.Request.Headers?.ContainsKey("X-Forwarded-For") == false)
                {
                    Log.Information("Not set nginx header.");
                    await next(context);
                }

                var ip = context.Request.Headers["X-Forwarded-For"].ToString().Split(',')
                    .FirstOrDefault();

                Log.Information($"ip:{ip}");

                if (context.Request.Method.ToUpper() == "POST" && context.Request.ContentType.Contains("form"))
                {
                    var from = context.Request.Form;
                    foreach (var keyValuePair in from)
                    {
                        Log.Information($"key:{keyValuePair.Key}, value:{keyValuePair.Value}");
                    }

                    var serv = context.RequestServices.GetRequiredService<IAppleAuthAppService>();
                    var res = serv.ReceiveTestAsync(new AppleAuthDto()
                    {
                        Code = from["code"],
                        State = from["state"],
                        Id_token = from["id_token"]
                    });
                    
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsJsonAsync(res);
                    // app.MapPost("/",
                    //     ([FromForm] AppleAuthDto appleAuthDto, [FromServices] IAppleAuthAppService testService) =>
                    //     {
                    //         testService.ReceiveTestAsync(appleAuthDto);
                    //     }
                    // );
                }

                await next(context);
            });

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