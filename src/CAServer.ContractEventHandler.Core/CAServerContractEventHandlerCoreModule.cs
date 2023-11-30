using CAServer.ContractEventHandler.Core.Application;
using CAServer.Grains.Grain.ValidateOriginChainId;
using CAServer.Signature;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Hangfire.Redis.StackExchange;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Hangfire;
using Volo.Abp.BackgroundJobs.Hangfire;
using Volo.Abp.Threading;

namespace CAServer.ContractEventHandler.Core;

[DependsOn(typeof(AbpAutoMapperModule), typeof(CAServerSignatureModule), typeof(CAServerApplicationModule),typeof(AbpBackgroundJobsHangfireModule))]

public class CAServerContractEventHandlerCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            //Add all mappings defined in the assembly of the MyModule class
            options.AddMaps<CAServerContractEventHandlerCoreModule>();
        });

        var configuration = context.Services.GetConfiguration();
        Configure<CrossChainOptions>(configuration.GetSection("CrossChain"));
        Configure<SyncOriginChainIdOptions>(configuration.GetSection("SyncOriginChainId"));
        Configure<BlockInfoOptions>(configuration.GetSection("BlockInfo"));
        context.Services.AddSingleton(new GraphQLHttpClient(configuration["GraphQL:Configuration"],
            new NewtonsoftJsonSerializer()));
        context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());
        ConfigureHangfire(context, configuration);
        

    }

    public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
    {
        // var service = context.ServiceProvider.GetRequiredService<ICrossChainTransferAppService>();
        // AsyncHelper.RunSync(async () => await service.ResetRetryTimesAsync());
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
}