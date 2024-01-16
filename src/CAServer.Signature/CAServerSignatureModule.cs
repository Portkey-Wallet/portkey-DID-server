using CAServer.Signature.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace CAServer.Signature;

public class CAServerSignatureModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<SignatureServerOptions>(context.Services.GetConfiguration().GetSection("SignatureServer"));
    }
}