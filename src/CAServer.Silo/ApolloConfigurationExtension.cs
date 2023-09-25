using Microsoft.Extensions.Configuration;

namespace CAServer.Silo;

using Microsoft.Extensions.Hosting;

public static class ApolloConfigurationExtension
{
    public static IHostBuilder UseApollo(this IHostBuilder builder)
    {
        return builder
            .ConfigureAppConfiguration((config) => { config.AddApollo(config.Build().GetSection("apollo")); });
    }
}