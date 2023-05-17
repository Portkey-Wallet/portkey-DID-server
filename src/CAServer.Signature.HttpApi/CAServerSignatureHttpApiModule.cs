using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;

namespace CAServer.Signature;

[DependsOn(
    typeof(AbpAspNetCoreMvcModule))]
public class CAServerSignatureHttpApiModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(CAServerSignatureHttpApiModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CAServerSignatureHttpApiModule>(); });
        var configuration = context.Services.GetConfiguration();
        Configure<SignatureOptions>(configuration.GetSection("Signature"));
    }
}