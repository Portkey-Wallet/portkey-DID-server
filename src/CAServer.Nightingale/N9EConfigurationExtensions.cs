using CAServer.Nightingale.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CAServer.Nightingale;

public static class N9EConfigurationExtensions
{
    /// <summary>
    /// add nightingale monitoring
    /// </summary>
    /// <param name="hostBuilder"></param>
    public static IHostBuilder UseNightingaleMonitoring(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(ConfigureServices);
    }
    
    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.Configure<N9EOptions>(options => options.AddClients<N9EClientForLogging>());
        services.Configure<N9EClientForLoggingOptions>(
            context.Configuration.GetSection("N9EClientForLogging"));
        services.TryAddSingleton<N9EClientFactory>();
    }
}