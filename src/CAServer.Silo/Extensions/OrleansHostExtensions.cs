
using System.Net;
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
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        var configSection = configuration.GetSection("Orleans");
        if (configSection == null)
            throw new ArgumentNullException(nameof(configSection), "The OrleansServer node is missing");
        // return hostBuilder;
        return hostBuilder.UseOrleans(siloBuilder =>
        {
            //Configure OrleansSnapshot
            siloBuilder
                .ConfigureEndpoints(advertisedIP: IPAddress.Parse(configSection.GetValue<string>("AdvertisedIP")),
                    siloPort: configSection.GetValue<int>("SiloPort"), gatewayPort: configSection.GetValue<int>("GatewayPort"), listenOnAnyHostAddress: true)
                .UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configSection.GetValue<string>("DataBase");
                    ;
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
                .AddMongoDBGrainStorage("Default", (MongoDBGrainStorageOptions op) =>
                {
                    op.CollectionPrefix = "GrainStorage";
                    op.DatabaseName = configSection.GetValue<string>("DataBase");
                    // op.ConfigureJsonSerializerSettings = jsonSettings =>
                    // {
                    //     // jsonSettings.ContractResolver = new PrivateSetterContractResolver();
                    //     jsonSettings.NullValueHandling = NullValueHandling.Include;
                    //     jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;
                    //     jsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                    // };

                })
                .UseMongoDBReminders(options =>
                {
                    options.DatabaseName = configSection.GetValue<string>("DataBase");
                    options.CreateShardKeyForCosmos = false;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configSection.GetValue<string>("ClusterId");
                    options.ServiceId = configSection.GetValue<string>("ServiceId");
                })
                // .UseLinuxEnvironmentStatistics()
                .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); });
               // .AddMemoryGrainStorage("PubSubStore")
                // .ConfigureApplicationParts(parts => parts.AddFromApplicationBaseDirectory())
                if (configSection.GetValue<bool>("UseDashboard", false))
                {
                    siloBuilder.UseDashboard(options =>
                    {
                        options.Username = configSection.GetValue<string>("DashboardUserName");
                        options.Password = configSection.GetValue<string>("DashboardPassword");
                        options.Host = "*";
                        options.Port = configSection.GetValue<int>("DashboardPort");
                        options.HostSelf = true;
                        options.CounterUpdateIntervalMs = configSection.GetValue<int>("DashboardCounterUpdateIntervalMs");
                    });
                }
        });
    }
}