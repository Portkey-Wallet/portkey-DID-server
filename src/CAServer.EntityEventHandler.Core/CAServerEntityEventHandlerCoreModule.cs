using Volo.Abp.Modularity;
using Volo.Abp.AutoMapper;

namespace CAServer.EntityEventHandler.Core
{
    [DependsOn(typeof(AbpAutoMapperModule))]
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
