using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace CAServer.Nightingale.Orleans.TelemetryConsumers
{
    public static class N9ETelemetryConsumerConfigurationExtensions
    {
        /// <summary>
        /// Adds a metrics telemetric consumer provider of type <see cref="N9ETelemetryConsumer"/>.
        /// </summary>
        /// <param name="hostBuilder"></param>
        public static ISiloHostBuilder AddNightingaleTelemetryConsumer(this ISiloHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices(ConfigureServices);
        }

        /// <summary>
        /// Adds a metrics telemetric consumer provider of type <see cref="N9ETelemetryConsumer"/>.
        /// </summary>
        /// <param name="hostBuilder"></param>
        public static ISiloBuilder AddNightingaleTelemetryConsumer(this ISiloBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices(ConfigureServices);
        }

        /// <summary>
        /// Adds a metrics telemetric consumer provider of type <see cref="N9ETelemetryConsumer"/>.
        /// </summary>
        /// <param name="clientBuilder"></param>
        public static IClientBuilder AddNightingaleTelemetryConsumer(this IClientBuilder clientBuilder)
        {
            return clientBuilder.ConfigureServices(ConfigureServices);
        }

        private static void ConfigureServices(Microsoft.Extensions.Hosting.HostBuilderContext context,
            IServiceCollection services)
        {
            services.Configure<TelemetryOptions>(options => options.AddConsumer<N9ETelemetryConsumer>());
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.Configure<TelemetryOptions>(options => options.AddConsumer<N9ETelemetryConsumer>());
        }
    }
}