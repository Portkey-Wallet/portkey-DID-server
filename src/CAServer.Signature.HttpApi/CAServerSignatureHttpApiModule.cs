using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;
using SignatureServer.Options;
using SignatureServer.Providers;
using Volo.Abp;
using Volo.Abp.AutoMapper;

namespace CAServer.Signature;

[DependsOn(
    typeof(AbpAspNetCoreMvcModule))]
public class CAServerSignatureHttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CAServerSignatureHttpApiModule>(); });
        var configuration = context.Services.GetConfiguration();
        Configure<KeyPairInfoOptions>(configuration.GetSection("KeyPairInfo"));
        Configure<KeyStoreOptions>(configuration.GetSection("KeyStore"));

        context.Services.AddSingleton<AccountProvider>();
    }
    
    
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        _ = context.ServiceProvider.GetService<AccountProvider>();
    }
    
}