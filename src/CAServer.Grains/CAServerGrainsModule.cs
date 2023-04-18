using CAServer.Grains.Grain.Account;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace CAServer.Grains;

[DependsOn(typeof(CAServerApplicationContractsModule), typeof(AbpAutoMapperModule))]
public class CAServerGrainsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CAServerGrainsModule>(); });

        var configuration = context.Services.GetConfiguration();
        var connStr = configuration["GraphQL:Configuration"];

        context.Services.Configure<CAAccountOption>(configuration.GetSection("CAAccountSetting"));

        //context.Services.AddTransient<ITokenPriceProvider>();
    }
}