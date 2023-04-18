using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace CAServer.ContractEventHandler.Core;

[DependsOn(typeof(AbpAutoMapperModule))]
public class CAServerContractEventHandlerCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            //Add all mappings defined in the assembly of the MyModule class
            options.AddMaps<CAServerContractEventHandlerCoreModule>();
        });
    }
}