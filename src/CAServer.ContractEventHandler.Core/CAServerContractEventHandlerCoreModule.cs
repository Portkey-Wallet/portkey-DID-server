using CAServer.ContractEventHandler.Core.Application;
using CAServer.Signature;
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
    }

    public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
    {
        // var service = context.ServiceProvider.GetRequiredService<ICrossChainTransferAppService>();
        // AsyncHelper.RunSync(async () => await service.ResetRetryTimesAsync());
    }
}