using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Statistics;
using HostBuilderContext = Microsoft.Extensions.Hosting.HostBuilderContext;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace CAServer.Silo.Extensions;

public static class OrleansHostExtensions
{
    public static IHostBuilder UseOrleansSnapshot(this IHostBuilder hostBuilder)
    {
#if DEBUG
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        var configSection = configuration.GetSection("Orleans");
        if (configSection == null)
            throw new ArgumentNullException(nameof(configSection), "The OrleansServer node is missing");
#endif
        
        return hostBuilder.UseOrleans((context, siloBuilder) =>
        {
            //Configure OrleansSnapshot
            var orleansConfigSection = context.Configuration.GetSection("Orleans");
            siloBuilder
                .ConfigureEndpoints(advertisedIP: IPAddress.Parse(orleansConfigSection.GetValue<string>("AdvertisedIP")),
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
                    options.ClusterId = orleansConfigSection.GetValue<string>("ClusterId");
                    options.ServiceId = orleansConfigSection.GetValue<string>("ServiceId");
                })
                // .AddMemoryGrainStorage("PubSubStore")
                .ConfigureApplicationParts(parts => parts.AddFromApplicationBaseDirectory())
                .UseDashboard(options =>
                {
                    options.Username = orleansConfigSection.GetValue<string>("DashboardUserName");
                    options.Password = orleansConfigSection.GetValue<string>("DashboardPassword");
                    options.Host = "*";
                    options.Port = orleansConfigSection.GetValue<int>("DashboardPort");
                    options.HostSelf = true;
                    options.CounterUpdateIntervalMs = orleansConfigSection.GetValue<int>("DashboardCounterUpdateIntervalMs");
                })
                .UseLinuxEnvironmentStatistics()
                .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); });
        });
    }
}