using AElf.ExceptionHandler;
using CAServer.CAActivity.Provider;
using CAServer.Grains;
using CAServer.MongoDB;
using CAServer.Options;
using CAServer.Signature.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Provider;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.CosmosDB;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using MassTransit;
using Medallion.Threading;
using Medallion.Threading.Redis;
using MongoDB.Driver;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.BackgroundJobs.Hangfire;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Volo.Abp.Threading;
using ChainOptions = CAServer.Grains.Grain.ApplicationHandler.ChainOptions;
using TransactionOptions = CAServer.BackGround.Options.TransactionOptions;

namespace CAServer.BackGround;

[DependsOn(
    typeof(AbpAccountApplicationModule),
    typeof(CAServerApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(CAServerGrainsModule),
    typeof(CAServerApplicationModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreMvcModule),
    typeof(CAServerMongoDbModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpBackgroundJobsHangfireModule),
    typeof(AOPExceptionModule),
    typeof(AbpEventBusRabbitMqModule)
)]
public class CABackGroundModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CABackGroundModule>(); });

        var configuration = context.Services.GetConfiguration();
        ConfigureOrleans(context, configuration);
        ConfigureHangfire(context, configuration);
        ConfigureGraphQl(context, configuration);

        // context.Services.AddSingleton<ITokenPriceProvider, TokenPriceProvider>();
        context.Services.AddSingleton<IThirdPartOrderProvider, ThirdPartOrderProvider>();
        context.Services.AddSingleton<IActivityProvider, ActivityProvider>();
        context.Services.AddSingleton<IHostedService, InitJobsService>();
        Configure<TransactionOptions>(configuration.GetSection("Transaction"));
        Configure<ChainOptions>(configuration.GetSection("Chains"));
        Configure<SignatureServerOptions>(context.Services.GetConfiguration().GetSection("SignatureServer"));
        ConfigureTokenCleanupService();
        ConfigureDistributedLocking(context, configuration);
        ConfigureMassTransit(context, configuration);
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
                // cfg.ReceiveEndpoint("SubscribeQueue_" + rabbitMqConfig.ClientId, , e =>
                // {
                //     e.ConfigureConsumer<OrderWsBroadcastConsumer>(ctx);
                // });
            });
        });
    }
    
    private void ConfigureHangfire(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var mongoType = configuration["Hangfire:MongoType"];
        var connectionString = configuration["Hangfire:ConnectionString"];
        if(connectionString.IsNullOrEmpty()) return;

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

            context.Services.AddHangfireServer(opt => { opt.Queues = new[] { "default", "notDefault" }; });
        }
    }

    private void ConfigureGraphQl(ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        context.Services.AddSingleton(new GraphQLHttpClient(configuration["GraphQL:Configuration"],
            new NewtonsoftJsonSerializer()));
        context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());
    }

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
    }

    private static void ConfigureOrleans(ServiceConfigurationContext context, IConfiguration configuration)
    {
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
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                .Build();
        });
    }


    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        // var env = context.GetEnvironment();
        //
        // app.UseRouting();
        // app.UseCors();
        // app.UseAuthentication();
        //
        // app.UseAuthorization();
        //
        // app.UseAuditing();
        // app.UseAbpSerilogEnrichers();
        // app.UseUnitOfWork();
        // app.UseConfiguredEndpoints();
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            // IsReadOnlyFunc = (DashboardContext context) => true
        });

        StartOrleans(context.ServiceProvider);
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        StopOrleans(context.ServiceProvider);
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
}