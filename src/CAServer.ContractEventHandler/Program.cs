using System;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Nightingale;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace CAServer.ContractEventHandler
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Log.Logger = LogHelper.CreateLogger(LogEventLevel.Debug);
            
            try
            {
                Log.Information("Starting CAServer.ContractEventHandler.");
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
                .UseApolloForHostBuilder()
                .ConfigureAppConfiguration(build => { build.AddJsonFile("appsettings.secrets.json", optional: true); })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddApplication<CAServerContractEventHandlerModule>();
                })
                .UseNightingaleMonitoring()
                .UseAutofac()
                .UseOrleansClient()
                .UseSerilog();
    }
}