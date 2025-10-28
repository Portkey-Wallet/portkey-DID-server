using CAServer.Grains;
using CAServer.Hub;
using CAServer.Localization;
using CAServer.MongoDB;
using CAServer.MultiTenancy;
using CAServer.Options;
using CAServer.Redis;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Localization.Resources.AbpUi;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.MultiTenancy;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.PermissionManagement.HttpApi;
using Volo.Abp.SettingManagement;
using Volo.Abp.Swashbuckle;
using Volo.Abp.TenantManagement;
using Volo.Abp.Threading;

namespace CAServer.AppleMigrate;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AbpDistributedLockingModule),
    typeof(AbpAspNetCoreMvcUiMultiTenancyModule),
    typeof(CAServerApplicationModule),
    typeof(CAServerMongoDbModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(CAServerApplicationContractsModule),
    typeof(AbpSwashbuckleModule),
    typeof(CAServerApplicationContractsModule),
    typeof(AbpAccountHttpApiModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(AbpPermissionManagementHttpApiModule),
    typeof(AbpTenantManagementHttpApiModule),
    typeof(AbpFeatureManagementHttpApiModule),
    typeof(AbpSettingManagementHttpApiModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(CAServerRedisModule)
)]
public class AppleMigrateModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        ConfigureConventionalControllers();
        ConfigureCache(configuration);
        ConfigureDataProtection(context, configuration, hostingEnvironment);
        
        ConfigureDistributedLocking(context, configuration);
        // ConfigureOrleans(context, configuration);
        ConfigureLocalization();
        ConfigureSwaggerServices(context, configuration);
    }
    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<CAServerResource>()
                .AddBaseTypes(
                    typeof(AbpUiResource)
                );
        });
    }

    private static void ConfigureSwaggerServices(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAbpSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "CAServer Apple Migrate API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            }
        );
    }
    
    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "CAServer:"; });
    }
    
    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(CAServerApplicationModule).Assembly);
        });
    }

    // private static void ConfigureOrleans(ServiceConfigurationContext context, IConfiguration configuration)
    // {
    //     context.Services.AddSingleton<IClusterClient>(o =>
    //     {
    //         return new ClientBuilder()
    //             .ConfigureDefaults()
    //             .UseMongoDBClient(configuration["Orleans:MongoDBClient"])
    //             .UseMongoDBClustering(options =>
    //             {
    //                 options.DatabaseName = configuration["Orleans:DataBase"];
    //                 ;
    //                 options.Strategy = MongoDBMembershipStrategy.SingleDocument;
    //             })
    //             .Configure<ClusterOptions>(options =>
    //             {
    //                 options.ClusterId = configuration["Orleans:ClusterId"];
    //                 options.ServiceId = configuration["Orleans:ServiceId"];
    //             })
    //             .ConfigureApplicationParts(parts =>
    //                 parts.AddApplicationPart(typeof(CAServerGrainsModule).Assembly).WithReferences())
    //             .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
    //             .Build();
    //     });
    // }

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

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        app.UseDeveloperExceptionPage();
        app.UseAbpRequestLocalization();
        app.UseCorrelationId();
        app.UseStaticFiles();
        app.UseRouting();
        
        app.UseSwagger();
        app.UseAbpSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "CAServer AppleMigrate API");
        });
        
        app.UseConfiguredEndpoints();
        // StartOrleans(context.ServiceProvider);
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        // StopOrleans(context.ServiceProvider);
    }

    // private static void StartOrleans(IServiceProvider serviceProvider)
    // {
    //     var client = serviceProvider.GetRequiredService<IClusterClient>();
    //     AsyncHelper.RunSync(async () => await client.Connect());
    // }
    //
    // private static void StopOrleans(IServiceProvider serviceProvider)
    // {
    //     var client = serviceProvider.GetRequiredService<IClusterClient>();
    //     AsyncHelper.RunSync(client.Close);
    // }
}