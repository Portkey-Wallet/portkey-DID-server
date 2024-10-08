using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Indexing.Elasticsearch.Options;
using CAServer.Cache;
using CAServer.CoinGeckoApi;
using CAServer.Commons;
using CAServer.CryptoGift;
using CAServer.EntityEventHandler.Core;
using CAServer.EntityEventHandler.Core.Worker;
using CAServer.Grains;
using CAServer.MongoDB;
using CAServer.Nightingale.Orleans.Filters;
using CAServer.Options;
using CAServer.Redis;
using CAServer.Tokens.TokenPrice.Provider.FeiXiaoHao;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using MassTransit;
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
    typeof(CAServerCoinGeckoApiModule),
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
        Configure<CAServer.Grains.Grain.ApplicationHandler.ChainOptions>(configuration.GetSection("Chains"));
        Configure<ImServerOptions>(configuration.GetSection("ImServer"));
        Configure<TokenPriceWorkerOption>(configuration.GetSection("TokenPriceWorker"));
        Configure<FeiXiaoHaoOptions>(configuration.GetSection("FeiXiaoHao"));
        Configure<CryptoGiftOptions>(configuration.GetSection("CryptoGiftExpiration"));
        Configure<ReferralRefreshTimeOptions>(configuration.GetSection("ReferralRefreshTime"));
        Configure<ActivityDateRangeOptions>(configuration.GetSection("ActivityDateRange"));
        Configure<ChatBotOptions>(configuration.GetSection("ChatBot"));
        Configure<ActivityConfigOptions>(configuration.GetSection("ActivityConfigs"));
        Configure<HamsterOptions>(configuration.GetSection("Hamster"));
        Configure<TonGiftsOptions>(configuration.GetSection("TonGifts"));

        ConfigureCache(configuration);
        ConfigureGraphQl(context, configuration);
        ConfigureDistributedLocking(context, configuration);
        ConfigureMassTransit(context, configuration);
        ConfigureRedisCacheProvider(context, configuration);
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
                .Configure<ClientMessagingOptions>(options =>
                {
                    options.ResponseTimeout =
                        TimeSpan.FromSeconds(Commons.ConfigurationHelper.GetValue("Orleans:ResponseTimeout",
                            MessagingOptions.DEFAULT_RESPONSE_TIMEOUT.Seconds));
                })
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(CAServerGrainsModule).Assembly).WithReferences())
                //.AddSimpleMessageStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                .AddNightingaleMethodFilter(o)
                .Build();
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
    
    private void ConfigureRedisCacheProvider(
        ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        var multiplexer = ConnectionMultiplexer
            .Connect(configuration["Redis:Configuration"]);
        context.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        context.Services.AddSingleton<ICacheProvider,RedisCacheProvider>();
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
    
    private void ConfigureMassTransit(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddMassTransit(x =>
        {
            var rabbitMqConfig = configuration.GetSection("RabbitMQ").Get<RabbitMqOptions>();
            // x.AddConsumer<OrderWsBroadcastConsumer>();
            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(rabbitMqConfig.Connections.Default.HostName, (ushort)rabbitMqConfig.Connections.Default.Port, 
                    "/", h =>
                    {
                        h.Username(rabbitMqConfig.Connections.Default.UserName);
                        h.Password(rabbitMqConfig.Connections.Default.Password);
                    });
                //
                // cfg.ReceiveEndpoint("SubscribeQueue_" + rabbitMqConfig.ClientId, e =>
                // {
                //     e.ConfigureConsumer<OrderWsBroadcastConsumer>(ctx);
                // });
            });
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
        backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<TokenPriceBackgroundWorker>());
        backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<CryptoGiftPreGrabQuotaExpiredWorker>());
        // backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<CryptoGiftStatisticsWorker>());
        //backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<InitReferralRankWorker>());
        backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<ReferralRankWorker>());
        backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<InitAddChatBotContactsWorker>());
        backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<HamsterActivityWorker>());
        backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<HamsterDataRepairWorker>());
        backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<TonGiftsValidateWorker>());
        
        
        ConfigurationProvidersHelper.DisplayConfigurationProviders(context);
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