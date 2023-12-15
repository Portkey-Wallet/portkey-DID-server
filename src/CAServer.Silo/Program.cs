using CAServer.Grains.Strategy;
using CAServer.Silo;
using CAServer.Silo.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Runtime.Placement;
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
            .ConfigureServices((hostcontext, services) =>
            {
                services.AddApplication<CAServerOrleansSiloModule>();
                services.AddSingletonNamedService<PlacementStrategy, FastStrategy>(nameof(FastStrategy));
                services.AddSingletonKeyedService<Type, IPlacementDirector, FastStrategyFixedSiloDirector>(
                    typeof(FastStrategyAttribute));
                services.AddSingletonKeyedService<Type, IPlacementDirector, SlowStrategyFixedSiloDirector>(
                    typeof(StatelessWorkerAttribute));
            })
            .UseOrleansSnapshot()
            .UseAutofac()
            .UseSerilog();
}