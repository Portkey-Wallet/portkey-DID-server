using CAServer.Grains.Grain.Account;
using CAServer.Options;
using CAServer.Signature;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace CAServer.Grains;

[DependsOn(typeof(CAServerApplicationContractsModule),
    typeof(AbpAutoMapperModule),
    typeof(CAServerSignatureModule))]
public class CAServerGrainsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CAServerGrainsModule>(); });

        var configuration = context.Services.GetConfiguration();
        var connStr = configuration["GraphQL:Configuration"];

        context.Services.Configure<CAAccountOption>(configuration.GetSection("CAAccountSetting"));
    }
}