using System;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Commons;
using CAServer.ContractEventHandler.Core;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.ContractEventHandler.Core.Worker;
using CAServer.EntityEventHandler.Core.Worker;
using CAServer.Grains;
using CAServer.MongoDB;
using CAServer.Monitor;
using CAServer.Nightingale.Orleans.Filters;
using CAServer.Options;
using CAServer.Signature;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Mongo;
using Hangfire.Mongo.CosmosDB;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
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
using ChainOptions = CAServer.ContractEventHandler.Core.Application.ChainOptions;
using ContractProvider = CAServer.ContractEventHandler.Core.Application.ContractProvider;
using IContractProvider = CAServer.ContractEventHandler.Core.Application.IContractProvider;
using StackExchange.Redis;
using Volo.Abp.BackgroundJobs.Hangfire;

namespace CAServer.ContractEventHandler;

[DependsOn(
    typeof(CAServerContractEventHandlerCoreModule),
    typeof(CAServerGrainsModule),
    typeof(AbpAutofacModule),
    // typeof(AbpBackgroundWorkersQuartzModule),
    typeof(AbpBackgroundWorkersModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(CAServerSignatureModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(CAServerMongoDbModule),
    typeof(CAServerMonitorModule),
    typeof(AbpBackgroundJobsHangfireModule),
    typeof(AElfIndexingElasticsearchModule)
)]
public class CAServerContractEventHandlerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        //ConfigureEsIndexCreation();   
        Configure<ChainOptions>(configuration.GetSection("Chains"));
        Configure<ImServerOptions>(configuration.GetSection("ImServer"));
        Configure<ContractSyncOptions>(configuration.GetSection("Sync"));
        Configure<IndexOptions>(configuration.GetSection("Index"));
        Configure<PayRedPackageAccount>(configuration.GetSection("RedPackagePayAccount"));
        Configure<GraphQLOptions>(configuration.GetSection("GraphQL"));
        Configure<GrabRedPackageOptions>(configuration.GetSection("GrabRedPackage"));
        Configure<NFTTraitsSyncOptions>(configuration.GetSection("NFTTraitsSync"));
        Configure<TransactionReportOptions>(configuration.GetSection("TransactionReport"));
        Configure<SyncChainHeightOptions>(configuration.GetSection("SyncChainHeight"));
        context.Services.AddHostedService<CAServerContractEventHandlerHostedService>();
        ConfigureOrleans(context, configuration);
        ConfigureTokenCleanupService();
        context.Services.AddSingleton<IContractAppService, ContractAppService>();
        context.Services.AddSingleton<IContractProvider, ContractProvider>();
        context.Services.AddSingleton<IGraphQLProvider, GraphQLProvider>();
        context.Services.AddSingleton<IRecordsBucketContainer, RecordsBucketContainer>();
        context.Services.AddSingleton<IRedPackageCreateResultService, RedPackageCreateResultService>();
        context.Services.AddSingleton<IHttpClientProvider, HttpClientProvider>();
        context.Services.AddSingleton<IPayRedPackageService, PayRedPackageService>();
        context.Services.AddHttpClient();
        ConfigureCache(configuration);
        ConfigureDataProtection(context, configuration, hostingEnvironment);
        ConfigureDistributedLocking(context, configuration);
        ConfigureHangfire(context, configuration);
        // ConfigureOpenTelemetry(context);
    }

    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "CAServer:"; });
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

    private void ConfigureDataProtection(
        ServiceConfigurationContext context,
        IConfiguration configuration,
        IWebHostEnvironment hostingEnvironment)
    {
        var dataProtectionBuilder = context.Services.AddDataProtection().SetApplicationName("CAServer");
        if (!hostingEnvironment.IsDevelopment())
        {
            var redis = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]);
            dataProtectionBuilder.PersistKeysToStackExchangeRedis(redis, "CAServer-Protection-Keys");
        }
    }

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        StartOrleans(context.ServiceProvider);
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        //StartOrleans(context.ServiceProvider);
        context.AddBackgroundWorkerAsync<ContractSyncWorker>();
        context.AddBackgroundWorkerAsync<TransferAutoReceiveWorker>();
        context.AddBackgroundWorkerAsync<NftTraitsProportionCalculateWorker>();
        context.AddBackgroundWorkerAsync<ChainHeightWorker>();
        // context.AddBackgroundWorkerAsync<SendingTransactionInfoByEmailAfterApprovalWorker>();
        ConfigurationProvidersHelper.DisplayConfigurationProviders(context);
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        StopOrleans(context.ServiceProvider);
    }

    private static void ConfigureOrleans(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddSingleton(o =>
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
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                .AddNightingaleMethodFilter(o)
                .Build();
        });
    }


    private static void StartOrleans(IServiceProvider serviceProvider)
    {
        var client = serviceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(async () => await client.Connect());
    }

    private static void StopOrleans(IServiceProvider serviceProvider)
    {
        var client = serviceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(client.Close);
    }

    // TODO Temporary Needed fixed later.
    private void ConfigureTokenCleanupService()
    {
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }

    private void ConfigureHangfire(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var mongoType = configuration["Hangfire:MongoType"];
        var connectionString = configuration["Hangfire:ConnectionString"];
        if (connectionString.IsNullOrEmpty()) return;

        if (mongoType.IsNullOrEmpty() ||
            mongoType.Equals(MongoType.MongoDb.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            context.Services.AddHangfire(x =>
            {
                x.UseMongoStorage(connectionString, new MongoStorageOptions
                {
                    MigrationOptions = new MongoMigrationOptions
                    {
                        MigrationStrategy = new MigrateMongoMigrationStrategy(),
                        BackupStrategy = new CollectionMongoBackupStrategy()
                    },
                    CheckConnection = true,
                    CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection
                });
            });
        }
        else if (mongoType.Equals(MongoType.DocumentDb.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            context.Services.AddHangfire(config =>
            {
                var mongoUrlBuilder = new MongoUrlBuilder(connectionString);
                var mongoClient = new MongoClient(mongoUrlBuilder.ToMongoUrl());
                var opt = new CosmosStorageOptions
                {
                    MigrationOptions = new MongoMigrationOptions
                    {
                        BackupStrategy = new NoneMongoBackupStrategy(),
                        MigrationStrategy = new DropMongoMigrationStrategy(),
                    }
                };
                config.UseCosmosStorage(mongoClient, mongoUrlBuilder.DatabaseName, opt);
            });
        }

        context.Services.AddHangfireServer(opt =>
        {
            opt.SchedulePollingInterval = TimeSpan.FromMilliseconds(3000);
            opt.HeartbeatInterval = TimeSpan.FromMilliseconds(3000);
            opt.Queues = new[] { "default", "notDefault" };
        });
    }
}