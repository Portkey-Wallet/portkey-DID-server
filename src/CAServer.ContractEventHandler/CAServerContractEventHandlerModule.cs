using System;
using CAServer.ContractEventHandler.Core;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.ContractEventHandler.Core.Worker;
using CAServer.Grains;
using CAServer.Options;
using CAServer.Monitor;
using CAServer.Signature;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Threading;
using ChainOptions = CAServer.ContractEventHandler.Core.Application.ChainOptions;

namespace CAServer.ContractEventHandler;

[DependsOn(
    typeof(CAServerContractEventHandlerCoreModule),
    typeof(CAServerGrainsModule),
    typeof(AbpAutofacModule),
    typeof(AbpBackgroundWorkersQuartzModule),
    typeof(AbpBackgroundWorkersModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(CAServerSignatureModule),
    typeof(AbpCachingStackExchangeRedisModule))]
public class CAServerContractEventHandlerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        //ConfigureEsIndexCreation();   
        Configure<ChainOptions>(configuration.GetSection("Chains"));
        Configure<ContractSyncOptions>(configuration.GetSection("Sync"));
        Configure<IndexOptions>(configuration.GetSection("Index"));
        Configure<GraphQLOptions>(configuration.GetSection("GraphQL"));
        context.Services.AddHostedService<CAServerContractEventHandlerHostedService>();
        ConfigureOrleans(context, configuration);
        ConfigureTokenCleanupService();
        context.Services.AddSingleton<IContractAppService, ContractAppService>();
        context.Services.AddSingleton<IContractProvider, ContractProvider>();
        context.Services.AddSingleton<IGraphQLProvider, GraphQLProvider>();
        context.Services.AddSingleton<IRecordsBucketContainer, RecordsBucketContainer>();
        context.Services.AddHttpClient();
        ConfigureCache(configuration);
        ConfigureDataProtection(context, configuration, hostingEnvironment);
    }

    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "CAServer:"; });
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

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        StartOrleans(context.ServiceProvider);
        context.AddBackgroundWorkerAsync<ContractSyncWorker>();
        context.AddBackgroundWorkerAsync<TransferAutoReceiveWorker>();
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
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(CAServerGrainsModule).Assembly).WithReferences())
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
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
}