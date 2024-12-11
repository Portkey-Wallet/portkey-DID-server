
using System.Net;
using CAServer.Silo.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;

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
                .Configure<JsonGrainStateSerializerOptions>(options => options.ConfigureJsonSerializerSettings =
                    settings =>
                    {
                        settings.NullValueHandling = NullValueHandling.Include;
                        settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                        settings.DefaultValueHandling = DefaultValueHandling.Populate;
                    })
                .ConfigureServices(services => services.AddSingleton<IGrainStateSerializer, VerifierJsonGrainStateSerializer>())
                .AddCaServerMongoDBGrainStorage("Default", (MongoDBGrainStorageOptions op) =>
                {
                    op.CollectionPrefix = "GrainStorage";
                    op.DatabaseName = orleansConfigSection.GetValue<string>("DataBase");

                    var grainIdPrefix = orleansConfigSection
                        .GetSection("GrainSpecificIdPrefix").GetChildren().ToDictionary(o => o.Key.ToLower(), o => o.Value);
                    op.KeyGenerator = id =>
                    {
                        var grainType = id.Type.ToString();
                        if (grainIdPrefix.TryGetValue(grainType, out var prefix))
                        {
                            return $"{prefix}+{id.Key}";
                        }

                        return id.ToString();
                    };
                    op.CreateShardKeyForCosmos = orleansConfigSection.GetValue<bool>("CreateShardKeyForMongoDB", false);
                })
                .Configure<GrainCollectionOptions>(options =>
                {
                    // Override the value of CollectionAge to
                    var collection = orleansConfigSection.GetSection(nameof(GrainCollectionOptions.ClassSpecificCollectionAge))
                        .GetChildren();
                    foreach (var item in collection)
                    {
                        options.ClassSpecificCollectionAge[item.Key] = TimeSpan.FromSeconds(int.Parse(item.Value));
                    }
                })
                .Configure<GrainCollectionNameOptions>(options =>
                {
                    var collectionName = orleansConfigSection
                        .GetSection(nameof(GrainCollectionNameOptions.GrainSpecificCollectionName)).GetChildren();
                    options.GrainSpecificCollectionName = collectionName.ToDictionary(o => o.Key, o => o.Value);
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
                .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); });
               // .AddMemoryGrainStorage("PubSubStore")
                if (orleansConfigSection.GetValue<bool>("UseDashboard", false))
                {
                    siloBuilder.UseDashboard(options =>
                    {
                        options.Username = orleansConfigSection.GetValue<string>("DashboardUserName");
                        options.Password = orleansConfigSection.GetValue<string>("DashboardPassword");
                        options.Host = "*";
                        options.Port = orleansConfigSection.GetValue<int>("DashboardPort");
                        options.HostSelf = true;
                        options.CounterUpdateIntervalMs = orleansConfigSection.GetValue<int>("DashboardCounterUpdateIntervalMs");
                    });
                }
        });
    }
}