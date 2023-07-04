using System.Collections.Generic;
using System.Linq;
using AElf.Indexing.Elasticsearch.Options;
using CAServer.Commons;
using CAServer.EntityEventHandler.Core;
using CAServer.Grains;
using CAServer.MongoDB;
using CAServer.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Threading;

namespace CAServer;

[DependsOn(typeof(AbpAutofacModule),
    typeof(CAServerMongoDbModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(CAServerEntityEventHandlerCoreModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AbpEventBusRabbitMqModule))]
public class CAServerEntityEventHandlerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        ConfigureTokenCleanupService();
        //ConfigureEsIndexCreation();
        context.Services.AddHostedService<CAServerHostedService>();
        ConfigureCache();

        context.Services.AddSingleton<IClusterClient>(o =>
        {
            return new ClientBuilder()
                .ConfigureDefaults()
                .UseMongoDBClient(configuration["Orleans:MongoDBClient"])
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configuration["Orleans:DataBase"];
                    ;
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configuration["Orleans:ClusterId"];
                    options.ServiceId = configuration["Orleans:ServiceId"];
                })
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(CAServerGrainsModule).Assembly).WithReferences())
                //.AddSimpleMessageStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                .Build();
        });
    }
    
    private void ConfigureCache()
    {
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "CAServer:"; });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var tokenList = context.ServiceProvider.GetRequiredService<IOptions<TokenListOptions>>().Value;
        var cache = context.ServiceProvider.GetRequiredService<IDistributedCache<List<string>>>();
        cache.Set(CommonConstant.ResourceTokenKey, tokenList.UserToken.Select(t => t.Token.Symbol).Distinct().ToList());

        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(async () => await client.Connect());
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(client.Close);
    }

    //Create the ElasticSearch Index based on Domain Entity
    private void ConfigureEsIndexCreation()
    {
        Configure<IndexCreateOption>(x => { x.AddModule(typeof(CAServerDomainModule)); });
    }

    // TODO Temporary Needed fixed later.
    private void ConfigureTokenCleanupService()
    {
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }
}