using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch.Options;
using CAServer.Commons;
using CAServer.EntityEventHandler.Core;
using CAServer.EntityEventHandler.Core.Worker;
using CAServer.Grains;
using CAServer.MongoDB;
using CAServer.Options;
using CAServer.RedPackage;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundWorkers;
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
        Configure<CAServer.Options.ChainOptions>(configuration.GetSection("Chains"));
        Configure<RedPackageOptions>(configuration.GetSection("RedPackage"));
        Configure<ImServerOptions>(configuration.GetSection("ImServer"));
        ConfigureCache(configuration);
        ConfigureGraphQl(context, configuration);
        ConfigureDistributedLocking(context, configuration);
        ConfigureHangfire(context, configuration);
        
        context.Services.AddSingleton<IClusterClient>(o =>
        {
            return new ClientBuilder()
                .ConfigureDefaults()
                .UseMongoDBClient(configuration["Orleans:MongoDBClient"])
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configuration["Orleans:DataBase"];
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
    
    private void ConfigureHangfire(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddHangfire(config =>
        {
            config.UseRedisStorage(configuration["Hangfire:Redis:ConnectionString"], new RedisStorageOptions
            {
                Db = 1
            });
        });
        
        context.Services.AddHangfireServer(options =>
        {
            options.Queues = new[] { "redpackage" };
            options.WorkerCount = 8;
        });
    }
    
    private void ConfigureGraphQl(ServiceConfigurationContext context,
        IConfiguration configuration)
    {        
        Configure<GraphQLOptions>(configuration.GetSection("GraphQL"));
        context.Services.AddSingleton(new GraphQLHttpClient(configuration["GraphQL:Configuration"],
            new NewtonsoftJsonSerializer()));
        context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());
    }

    
    private void ConfigureDistributedLocking(
        ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        context.Services.AddSingleton<IDistributedLockProvider>(sp =>
        {
            var connection = ConnectionMultiplexer
                .Connect(configuration["Redis:Configuration"]);
            return new RedisDistributedSynchronizationProvider(connection.GetDatabase());
        });
    }

    
    private void ConfigureCache(IConfiguration configuration)
    {
        var cacheOptions = configuration.GetSection("Cache").Get<CacheOptions>();
        var expirationDays = cacheOptions?.ExpirationDays ?? CommonConstant.CacheExpirationDays;

        Configure<AbpDistributedCacheOptions>(options =>
        {
            options.KeyPrefix = "CAServer:";
            options.GlobalCacheEntryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(expirationDays)
            };
        });
    }

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        var tokenList = context.ServiceProvider.GetRequiredService<IOptions<TokenListOptions>>().Value;
        var cache = context.ServiceProvider.GetRequiredService<IDistributedCache<List<string>>>();
        cache.Set(CommonConstant.ResourceTokenKey, tokenList.UserToken.Select(t => t.Token.Symbol).Distinct().ToList());

        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(async () => await client.Connect());
    }
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var backgroundWorkerManger = context.ServiceProvider.GetRequiredService<IBackgroundWorkerManager>();
        backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<LoginGuardianChangeRecordReceiveWorker>());
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