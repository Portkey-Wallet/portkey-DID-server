using CAServer.Silo;
using CAServer.Silo.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using Serilog;
using Serilog.Configuration;
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
            // .WriteTo.Console(new ExpressionTemplate("[{@t:HH:mm:ss fff} {@l:u3}" +
            //                                         " Application:{Application},Module:{Module}{#if tracingId is not null},tracingId:{tracingId}{#end}{#if RequestPath is not null},RequestPath:{RequestPath}{#end}] {@m}\n{@x}"))
            .WriteTo.Console(new ExpressionTemplate("[{@t:HH:mm:ss fff} {@l:u3} {tracing_id} {Application} {Module} ] {@m}\n{@x}"))
            
            //.WriteTo.Async(c => c.Console())
#endif
            .CreateLogger();

        try
        {
            Log.Information("Starting CAServer.Silo.");

            await CreateHostBuilder(args).RunConsoleAsync();
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

    internal static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostcontext, services) => { services.AddApplication<CAServerOrleansSiloModule>(); })
            .UseOrleansSnapshot()
            .UseAutofac()
            .UseSerilog();
}