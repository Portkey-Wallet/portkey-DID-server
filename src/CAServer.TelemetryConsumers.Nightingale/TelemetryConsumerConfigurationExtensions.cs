using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Orleans.TelemetryConsumers.Nightingale
{
    public static class TelemetryConsumerConfigurationExtensions
    {
        /// <summary>
        /// Adds a metrics telemetric consumer provider of type <see cref="TelemetryConsumer"/>.
        /// </summary>
        /// <param name="hostBuilder"></param>
        public static ISiloHostBuilder AddNightingaleTelemetryConsumer(this ISiloHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices((context, services) => ConfigureServices(context, services));
        }

        /// <summary>
        /// Adds a metrics telemetric consumer provider of type <see cref="TelemetryConsumer"/>.
        /// </summary>
        /// <param name="hostBuilder"></param>
        public static ISiloBuilder AddNightingaleTelemetryConsumer(this ISiloBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices((context, services) => ConfigureServices(context, services));
        }

        /// <summary>
        /// Adds a metrics telemetric consumer provider of type <see cref="TelemetryConsumer"/>.
        /// </summary>
        /// <param name="clientBuilder"></param>
        public static IClientBuilder AddNightingaleTelemetryConsumer(this IClientBuilder clientBuilder)
        {
            return clientBuilder.ConfigureServices((context, services) => ConfigureServices(context, services));
        }

        private static void ConfigureServices(Microsoft.Extensions.Hosting.HostBuilderContext context,
            IServiceCollection services)
        {
            services.Configure<TelemetryOptions>(options => options.AddConsumer<TelemetryConsumer>());
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.Configure<TelemetryOptions>(options => options.AddConsumer<TelemetryConsumer>());
        }
    }
}