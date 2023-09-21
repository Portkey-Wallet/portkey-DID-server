using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CAServer;

public static class ApolloConfigurationExtension
{
    public static IHostBuilder UseApollo(this IHostBuilder builder)
    {
        return builder
            .ConfigureAppConfiguration((config) =>
            {
                config.AddApollo(config.Build().GetSection("apollo"));
            });
    }
}