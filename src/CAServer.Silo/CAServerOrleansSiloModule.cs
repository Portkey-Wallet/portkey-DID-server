using CAServer.CoinGeckoApi;
using CAServer.Commons;
using CAServer.Grains;
using CAServer.Grains.Grain.ApplicationHandler;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace CAServer.Silo;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    //typeof(CAServerApplicationModule),
    typeof(CAServerGrainsModule)
    //typeof(CABackGroundModule)
)]
public class CAServerOrleansSiloModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<CAServerHostedService>();
        var configuration = context.Services.GetConfiguration();
        //ConfigureEsIndexCreation();
        Configure<GrainOptions>(configuration.GetSection("Contract"));
        Configure<ChainOptions>(configuration.GetSection("Chains"));
        context.Services.AddHttpClient();
    }
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        ConfigurationProvidersHelper.DisplayConfigurationProviders(context);
    }
}