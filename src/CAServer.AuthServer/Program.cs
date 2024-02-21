﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CAServer;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            Log.Information("Starting CAServer.AuthServer.");
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.AddAppSettingsSecretsJson()
                .InitAppConfiguration(false)
                .UseApollo()
                .UseAutofac()
                .UseSerilog();

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
}