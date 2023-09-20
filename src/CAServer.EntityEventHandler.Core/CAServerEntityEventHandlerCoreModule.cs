using CAServer.ContractEventHandler.Core;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace CAServer.EntityEventHandler.Core
{
    [DependsOn(typeof(AbpAutoMapperModule), typeof(CAServerApplicationModule),
        typeof(CAServerApplicationContractsModule))]
    public class CAServerEntityEventHandlerCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpAutoMapperOptions>(options =>
            {
                //Add all mappings defined in the assembly of the MyModule class
                options.AddMaps<CAServerEntityEventHandlerCoreModule>();
            });
        }
    }
}