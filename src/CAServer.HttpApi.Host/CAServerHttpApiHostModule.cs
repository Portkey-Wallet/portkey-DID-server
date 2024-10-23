using System;
using System.Linq;
using CAServer.CoinGeckoApi;
using CAServer.Commons;
using CAServer.Grains;
using CAServer.Hub;
using CAServer.HubsEventHandler;
using CAServer.Middleware;
using CAServer.MongoDB;
using CAServer.Monitor.Interceptor;
using CAServer.MultiTenancy;
using CAServer.Nightingale.Http;
using CAServer.Nightingale.Orleans.Filters;
using CAServer.Options;
using CAServer.Redis;
using CAServer.ThirdPart.Adaptor;
using CAServer.Transfer;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using MassTransit;
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
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.MultiTenancy;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.Auditing;
using Volo.Abp.Autofac;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Swashbuckle;
using Volo.Abp.Threading;
using ConfigurationHelper = CAServer.Commons.ConfigurationHelper;

namespace CAServer;

[DependsOn(
    typeof(CAServerHttpApiModule),
    typeof(AbpAutofacModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AbpDistributedLockingModule),
    typeof(AbpAspNetCoreMvcUiMultiTenancyModule),
    typeof(CAServerApplicationModule),
    typeof(CAServerMongoDbModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(CAServerHubModule),
    typeof(CAServerRedisModule),
    typeof(AbpSwashbuckleModule),
    typeof(CAServerCoinGeckoApiModule),
    typeof(AbpAspNetCoreSignalRModule)
)]
public class CAServerHttpApiHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        Configure<RampOptions>(configuration.GetSection("RampOptions"));
        Configure<ChainOptions>(configuration.GetSection("Chains"));
        Configure<RealIpOptions>(configuration.GetSection("RealIp"));
        Configure<TransactionFeeOptions>(configuration.GetSection("TransactionFeeInfo"));
        Configure<Grains.Grain.ApplicationHandler.ChainOptions>(configuration.GetSection("Chains"));
        Configure<AddToWhiteListUrlsOptions>(configuration.GetSection("AddToWhiteListUrls"));
        Configure<ContactOptions>(configuration.GetSection("Contact"));
        Configure<ActivityTypeOptions>(configuration.GetSection("ActivityOptions"));
        Configure<IpWhiteListOptions>(configuration.GetSection("IpWhiteList"));
        Configure<AuthServerOptions>(configuration.GetSection("AuthServer"));
        Configure<HubConfigOptions>(configuration.GetSection("HubConfig"));
        Configure<TokenPriceWorkerOption>(configuration.GetSection("TokenPriceWorker"));
        Configure<PerformanceMonitorMiddlewareOptions>(configuration.GetSection("PerformanceMonitorMiddleware"));
        Configure<ChatBotOptions>(configuration.GetSection("ChatBot"));
        
        ConfigureConventionalControllers();
        ConfigureAuthentication(context, configuration);
        ConfigureLocalization();
        ConfigureCache(configuration);
        ConfigureVirtualFileSystem(context);
        ConfigureDataProtection(context, configuration, hostingEnvironment);
        ConfigureDistributedLocking(context, configuration);
        ConfigureHub(context, configuration);
        ConfigureGraphQl(context, configuration);
        ConfigureCors(context, configuration);
        ConfigureSwaggerServices(context, configuration);
        ConfigureOrleans(context, configuration);
        ConfigureOpenTelemetry(context); //config open telemetry info
        context.Services.AddHttpContextAccessor();
        ConfigureTokenCleanupService();
        ConfigureMassTransit(context, configuration);
        context.Services.AddSignalR().AddStackExchangeRedis(configuration["Redis:Configuration"],
            options => { options.Configuration.ChannelPrefix = "CAServer"; });
        ConfigAuditing();
    }
    
    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "CAServer:"; });
    }

    private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        // if (hostingEnvironment.IsDevelopment())
        // {
        //     Configure<AbpVirtualFileSystemOptions>(options =>
        //     {
        //         options.FileSets.ReplaceEmbeddedByPhysical<CAServerDomainSharedModule>(
        //             Path.Combine(hostingEnvironment.ContentRootPath,
        //                 $"..{Path.DirectorySeparatorChar}CAServer.Domain.Shared"));
        //         options.FileSets.ReplaceEmbeddedByPhysical<CAServerDomainModule>(
        //             Path.Combine(hostingEnvironment.ContentRootPath,
        //                 $"..{Path.DirectorySeparatorChar}CAServer.Domain"));
        //         options.FileSets.ReplaceEmbeddedByPhysical<CAServerApplicationContractsModule>(
        //             Path.Combine(hostingEnvironment.ContentRootPath,
        //                 $"..{Path.DirectorySeparatorChar}CAServer.Application.Contracts"));
        //         options.FileSets.ReplaceEmbeddedByPhysical<CAServerApplicationModule>(
        //             Path.Combine(hostingEnvironment.ContentRootPath,
        //                 $"..{Path.DirectorySeparatorChar}CAServer.Application"));
        //     });
        // }
    }

    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(CAServerApplicationModule).Assembly);
        });
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["AuthServer:Authority"];
                options.RequireHttpsMetadata = Convert.ToBoolean(configuration["AuthServer:RequireHttpsMetadata"]);
                options.Audience = "CAServer";
            });
    }

    private static void ConfigureSwaggerServices(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAbpSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "CAServer API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Scheme = "bearer",
                    Description = "Specify the authorization token.",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new string[] { }
                    }
                });
            }
        );
        // context.Services.AddAbpSwaggerGenWithOAuth(
        //     configuration["AuthServer:Authority"],
        //     new Dictionary<string, string>
        //     {
        //         { "CAServer", "CAServer API" }
        //     },
        //     options =>
        //     {
        //         options.SwaggerDoc("v1", new OpenApiInfo { Title = "CAServer API", ClientVersion = "v1" });
        //         options.DocInclusionPredicate((docName, description) => true);
        //         options.CustomSchemaIds(type => type.FullName);
        //     });
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
                .Configure<ClientMessagingOptions>(options =>
                {
                    //the default timeout before a request is assumed to have failed.
                    options.ResponseTimeout =
                        TimeSpan.FromSeconds(ConfigurationHelper.GetValue("Orleans:ResponseTimeout",
                            MessagingOptions.DEFAULT_RESPONSE_TIMEOUT.Seconds));
                })
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(CAServerGrainsModule).Assembly).WithReferences())
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                .AddNightingaleMethodFilter(o)
                .Build();
        });
    }

    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Languages.Add(new LanguageInfo("en", "en", "English"));
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

    private void ConfigureHub(
        ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        var multiplexer = ConnectionMultiplexer
            .Connect(configuration["Redis:Configuration"]);
        context.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        Configure<HubCacheOptions>(configuration.GetSection("Hub:Configuration"));
    }

    private void ConfigureGraphQl(ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        context.Services.AddSingleton(new GraphQLHttpClient(configuration["GraphQL:Configuration"],
            new NewtonsoftJsonSerializer()));
        context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(
                        configuration["App:CorsOrigins"]
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.RemovePostFix("/"))
                            .ToArray()
                    )
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    private void ConfigureMassTransit(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddMassTransit(x =>
        {
            var rabbitMqConfig = configuration.GetSection("RabbitMQ").Get<RabbitMqOptions>();
            var clientId = configuration.GetSection("ClientId").Get<string>();
            x.AddConsumer<OrderWsBroadcastConsumer>();
            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(rabbitMqConfig.Connections.Default.HostName, (ushort)rabbitMqConfig.Connections.Default.Port, 
                    "/", h =>
                    {
                        h.Username(rabbitMqConfig.Connections.Default.UserName);
                        h.Password(rabbitMqConfig.Connections.Default.Password);
                    });
                
                cfg.ReceiveEndpoint("BroadcastClient_" + clientId, e =>
                {
                    e.ConfigureConsumer<OrderWsBroadcastConsumer>(ctx);
                });
            });
        });
    }

    private void ConfigureTokenCleanupService()
    {
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }
    
    //enhance performance monitoring capability 
    private void ConfigureOpenTelemetry(ServiceConfigurationContext context)
    {
        IServiceCollection services = context.Services;
        services.OnRegistred(options =>
        {
            if (options.ImplementationType.IsDefined(typeof(MonitorAttribute), true))
            {
                options.Interceptors.TryAdd<MonitorInterceptor>();
            }
        });
        
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddSource("CAServer")
                    .SetSampler(new AlwaysOnSampler());
                // .AddAspNetCoreInstrumentation();
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter("CAServer")
                    // .AddAspNetCoreInstrumentation()
                    .AddPrometheusExporter();
            });
    }

    //Disables the auditing system
    private void ConfigAuditing()
    {
        Configure<AbpAuditingOptions>(options =>
        {
            options.IsEnabled = false;
        });
    }
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();
        app.UseCorrelationId();
        
        app.UseMiddleware<DeviceInfoMiddleware>();
        app.UseMiddleware<ConditionalIpWhitelistMiddleware>();
        app.UseMiddleware<PerformanceMonitorMiddleware>();
        app.UseStaticFiles();
        
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();

        if (MultiTenancyConsts.IsEnabled)
        {
            app.UseMultiTenancy();
        }

        app.UseAuthorization();
        if (!env.IsDevelopment())
        {
            app.UseMiddleware<RealIpMiddleware>();
        }
        
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseAbpSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "CAServer API");

                // var configuration = context.GetConfiguration();
                // options.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
                // options.OAuthSc+opes("CAServer");
            });
        }

        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseUnitOfWork();
        app.UseConfiguredEndpoints();
        app.UseOpenTelemetryPrometheusScrapingEndpoint();

        StartOrleans(context.ServiceProvider);

        // to start pre heat
        _ = context.ServiceProvider.GetService<TransakAdaptor>().PreHeatCachesAsync();
        context.ServiceProvider.GetService<IShiftChainService>().Init();

        ConfigurationProvidersHelper.DisplayConfigurationProviders(context);
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
}