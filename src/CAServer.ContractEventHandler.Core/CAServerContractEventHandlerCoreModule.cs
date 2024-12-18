using System;
using System.Collections.Generic;
using CAServer.Commons;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Grains.Grain.ValidateOriginChainId;
using CAServer.Signature;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace CAServer.ContractEventHandler.Core;

[DependsOn(typeof(AbpAutoMapperModule), typeof(CAServerSignatureModule),
    typeof(CAServerApplicationModule),
    typeof(CAServerApplicationContractsModule))]
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
        Configure<ZkLoginWorkerOptions>(configuration.GetSection("ZkLoginWorker"));
        Configure<ZkLoginWorkerOptions>(configuration.GetSection("NotifyWorker"));
        context.Services.AddSingleton(new GraphQLHttpClient(configuration["GraphQL:Configuration"],
            new NewtonsoftJsonSerializer()));
        context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());

        AddAelfClient(context, configuration);
    }

    private void AddAelfClient(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var chainInfos = configuration.GetValue<ChainOptions>("Chains");
        if (chainInfos == null || chainInfos.ChainInfos.IsNullOrEmpty())
        {
            return;
        }

        foreach (var chainInfo in chainInfos.ChainInfos)
        {
            var clientName = chainInfo.Key == CommonConstant.MainChainId
                ? AelfClientConstant.MainChainClient
                : AelfClientConstant.SideChainClient;

            context.Services.AddHttpClient(clientName,
                httpClient =>
                {
                    httpClient.BaseAddress = new Uri(chainInfo.Value.BaseUrl);
                    httpClient.Timeout = TimeSpan.FromSeconds(60);
                });
        }
    }

    public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
    {
        // var service = context.ServiceProvider.GetRequiredService<ICrossChainTransferAppService>();
        // AsyncHelper.RunSync(async () => await service.ResetRetryTimesAsync());
    }
}