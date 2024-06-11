using System;
using System.Threading.Tasks;
using CAServer.Commons;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace CAServer.RedPackage
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Log.Logger = LogHelper.CreateLogger(LogEventLevel.Debug);
            
            try
            {
                Log.Information("Starting CAServer.RedPackage.");
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
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddApplication<CAServerRedPackageModule>();
                })
                .UseAutofac()
                .UseSerilog();
    }
}