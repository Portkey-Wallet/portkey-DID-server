﻿using System;
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

            // app.MapPost("/",
            //     ([FromForm] AppleAuthDto appleAuthDto, [FromServices] IAppleAuthAppService testService) =>
            //     {
            //         testService.ReceiveTestAsync(appleAuthDto);
            //     }
            // );
            app.Use(async (context, next) =>
            {
                var path = context.Request.Host.Value;

                Log.Information($"path:{path}");

                if (path.Contains("apple"))
                {
                    app.MapPost("/",
                        ([FromForm] AppleAuthDto appleAuthDto, [FromServices] IAppleAuthAppService testService) =>
                        {
                            testService.ReceiveTestAsync(appleAuthDto);
                        }
                    );
                }

                // Call the next delegate/middleware in the pipeline.
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