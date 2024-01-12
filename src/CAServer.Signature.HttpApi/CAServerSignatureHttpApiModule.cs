using CAServer.Signature;
using Microsoft.Extensions.DependencyInjection;
using SignatureServer.Options;
using SignatureServer.Providers;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace SignatureServer;

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
        context.Services.AddSingleton<StorageProvider>();
    }
    
    
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        _ = context.ServiceProvider.GetService<AccountProvider>();
        _ = context.ServiceProvider.GetService<StorageProvider>();
    }
    
}