using System.Net;
using CAServer.Nightingale.Orleans.Filters;
using CAServer.Nightingale.Orleans.TelemetryConsumers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Statistics;

namespace CAServer.Silo.Extensions;

public static class OrleansHostExtensions
{
    public static IHostBuilder UseOrleansSnapshot(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseOrleans((context, siloBuilder) =>
        {
            //Configure OrleansSnapshot
            var orleansConfigSection = context.Configuration.GetSection("Orleans");
            var isRunningInKubernetes = orleansConfigSection.GetValue<bool>("isRunningInKubernetes");
            var advertisedIP = isRunningInKubernetes ?  Environment.GetEnvironmentVariable("POD_IP") :orleansConfigSection.GetValue<string>("AdvertisedIP");
            var clusterId = isRunningInKubernetes ? Environment.GetEnvironmentVariable("ORLEANS_CLUSTER_ID") : orleansConfigSection.GetValue<string>("ClusterId");
            var serviceId = isRunningInKubernetes ? Environment.GetEnvironmentVariable("ORLEANS_SERVICE_ID") : orleansConfigSection.GetValue<string>("ServiceId");

            siloBuilder
                .ConfigureEndpoints(
                    advertisedIP: IPAddress.Parse(advertisedIP),
                    siloPort: orleansConfigSection.GetValue<int>("SiloPort"),
                    gatewayPort: orleansConfigSection.GetValue<int>("GatewayPort"), listenOnAnyHostAddress: true)
                .UseMongoDBClient(orleansConfigSection.GetValue<string>("MongoDBClient"))
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = orleansConfigSection.GetValue<string>("DataBase");
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .AddMongoDBGrainStorage("Default", (MongoDBGrainStorageOptions op) =>
                {
                    op.CollectionPrefix = "GrainStorage";
                    op.DatabaseName = orleansConfigSection.GetValue<string>("DataBase");

                    op.ConfigureJsonSerializerSettings = jsonSettings =>
                    {
                        // jsonSettings.ContractResolver = new PrivateSetterContractResolver();
                        jsonSettings.NullValueHandling = NullValueHandling.Include;
                        jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;
                        jsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                    };
                })
                .UseMongoDBReminders(options =>
                {
                    options.DatabaseName = orleansConfigSection.GetValue<string>("DataBase");
                    options.CreateShardKeyForCosmos = false;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = clusterId;
                    options.ServiceId = serviceId;
                })
                .Configure<SiloMessagingOptions>(options =>
                {
                    options.ResponseTimeout =
                        TimeSpan.FromSeconds(Commons.ConfigurationHelper.GetValue("Orleans:ResponseTimeout",
                            MessagingOptions.DEFAULT_RESPONSE_TIMEOUT.Seconds));
                })
                // .AddMemoryGrainStorage("PubSubStore")
                .ConfigureApplicationParts(parts => parts.AddFromApplicationBaseDirectory())
                .Configure<GrainCollectionOptions>(opt =>
                {
                    var collectionAge = orleansConfigSection.GetValue<int>("CollectionAge");
                    if (collectionAge > 0)
                    {
                        opt.CollectionAge = TimeSpan.FromSeconds(collectionAge);
                    }
                })
                .Configure<PerformanceTuningOptions>(opt =>
                {
                    var minDotNetThreadPoolSize = orleansConfigSection.GetValue<int>("MinDotNetThreadPoolSize");
                    var minIOThreadPoolSize = orleansConfigSection.GetValue<int>("MinIOThreadPoolSize");
                    opt.MinDotNetThreadPoolSize = minDotNetThreadPoolSize > 0 ? minDotNetThreadPoolSize : 200;
                    opt.MinIOThreadPoolSize = minIOThreadPoolSize > 0 ? minIOThreadPoolSize : 200;
                })
                .UseLinuxEnvironmentStatistics()
                .AddNightingaleTelemetryConsumer()
                .AddNightingaleMethodFilter()
                .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); });
            if (orleansConfigSection.GetValue<bool>("UseDashboard", false))
            {
                siloBuilder.UseDashboard(options =>
                {
                    options.Username = orleansConfigSection.GetValue<string>("DashboardUserName");
                    options.Password = orleansConfigSection.GetValue<string>("DashboardPassword");
                    options.Host = "*";
                    options.Port = orleansConfigSection.GetValue<int>("DashboardPort");
                    options.HostSelf = true;
                    options.CounterUpdateIntervalMs =
                        orleansConfigSection.GetValue<int>("DashboardCounterUpdateIntervalMs");
                });
            }
        });
    }
}