using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;

namespace CAServer.Nightingale.Orleans.Filters;

public static class GrainMethodFilterExtensions
{
    /// <summary>
    /// add grain method invocation monitoring
    /// </summary>
    /// <param name="hostBuilder"></param>
    public static ISiloBuilder AddNightingaleMethodFilter(this ISiloBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(ConfigureServices);
    }
    
    /// <summary>
    /// add grain method invocation monitoring
    /// </summary>
    /// <param name="clientBuilder"></param>
    public static IClientBuilder AddNightingaleMethodFilter(this IClientBuilder clientBuilder, IServiceProvider serviceProvider)
    {
        return clientBuilder.ConfigureServices((context, services) =>
        {
            MethodFilterContext.ServiceProvider = serviceProvider;
            services.Configure<MethodCallFilterOptions>(context.Configuration.GetSection("MethodCallFilter"));
            services.AddSingleton<IOutgoingGrainCallFilter, MethodCallFilter>();;
        });
    }
    
    private static void ConfigureServices(Microsoft.Extensions.Hosting.HostBuilderContext context,
        IServiceCollection services)
    {
        services.Configure<MethodServiceFilterOptions>(context.Configuration.GetSection("MethodServiceFilter"));
        services.AddSingleton<IIncomingGrainCallFilter, MethodServiceFilter>();
    }
}