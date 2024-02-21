using CAServer.Silo.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CAServer.Silo;
public class Program
{
    public async static Task<int> Main(string[] args)
    {
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
            .InitAppConfiguration(true)
            .UseApolloForSilo()
            .ConfigureServices((hostcontext, services) => { services.AddApplication<CAServerOrleansSiloModule>(); })
            .UseOrleansSnapshot()
            .UseAutofac()
            .UseSerilog();
}