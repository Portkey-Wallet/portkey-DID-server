using System;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.ContractService;
using CAServer.Grains;
using CAServer.Monitor;
using CAServer.Nightingale.Orleans.Filters;
using CAServer.Options;
using CAServer.Signature;
using Medallion.Threading;
using Medallion.Threading.Redis;
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
using Volo.Abp.AutoMapper;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Threading;
using ChainOptions = CAServer.Options.ChainOptions;
using ContractProvider = CAServer.ContractEventHandler.Core.Application.ContractProvider;
using IContractProvider = CAServer.ContractEventHandler.Core.Application.IContractProvider;

namespace CAServer.RedPackage;
[DependsOn(
    typeof(CAServerGrainsModule),
    typeof(AbpAutofacModule),
    typeof(AbpBackgroundWorkersModule),
    typeof(AbpAspNetCoreSerilogModule),
    //typeof(AbpEventBusRabbitMqModule),
    typeof(CAServerSignatureModule),
    typeof(AbpCachingStackExchangeRedisModule),
    //typeof(CAServerMongoDbModule),
    typeof(AElfIndexingElasticsearchModule)
)]
public class CAServerRedPackageModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            //Add all mappings defined in the assembly of the MyModule class
            options.AddMaps<CAServerRedPackageModule>();
        });
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        Configure<CAServer.ContractEventHandler.Core.Application.ChainOptions>(configuration.GetSection("Chains"));
        Configure<CAServer.Options.ChainOptions>(configuration.GetSection("Chains"));
        Configure<ImServerOptions>(configuration.GetSection("ImServer"));
        Configure<ContractSyncOptions>(configuration.GetSection("Sync"));
        Configure<PayRedPackageAccount>(configuration.GetSection("RedPackagePayAccount"));
        Configure<GraphQLOptions>(configuration.GetSection("GraphQL"));
        Configure<GrabRedPackageOptions>(configuration.GetSection("GrabRedPackage"));
        Configure<RefundOptions>(configuration.GetSection("Refund"));
        ConfigureOrleans(context, configuration);
        ConfigureTokenCleanupService();
        context.Services.AddSingleton<IContractAppService, ContractAppService>();
        context.Services.AddSingleton<IContractProvider, ContractProvider>();
        context.Services.AddSingleton<IGraphQLProvider, GraphQLProvider>();
        context.Services.AddSingleton<IIndicatorScope, IndicatorScope>();
        // context.Services.AddSingleton<IRecordsBucketContainer, RecordsBucketContainer>();
        // context.Services.AddSingleton<IRedPackageCreateResultService, RedPackageCreateResultService>();
        context.Services.AddSingleton<IHttpClientProvider, HttpClientProvider>();
        context.Services.AddSingleton<IContractService, ContractService.ContractService>();
        // context.Services.AddSingleton<IPayRedPackageService, PayRedPackageService>();
        ConfigureCache(configuration);
        ConfigureDataProtection(context, configuration, hostingEnvironment);
        context.Services.AddHostedService<CAServerRedPackageHostedService>();
        ConfigureDistributedLocking(context, configuration);
        context.Services.AddHttpClient();
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
        context.AddBackgroundWorkerAsync<RefundWorker>();
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
    
    private void ConfigureTokenCleanupService()
    {
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }
    
}