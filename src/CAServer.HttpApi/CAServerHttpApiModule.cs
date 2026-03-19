using Localization.Resources.AbpUi;
using CAServer.IpInfo;
using CAServer.Localization;
using CAServer.Verifier;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.HttpApi;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Volo.Abp.EventBus.RabbitMq;

namespace CAServer;

[DependsOn(
    typeof(CAServerApplicationContractsModule),
    typeof(AbpAccountHttpApiModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(AbpPermissionManagementHttpApiModule),
    typeof(AbpTenantManagementHttpApiModule),
    typeof(AbpFeatureManagementHttpApiModule),
    typeof(AbpSettingManagementHttpApiModule),
    typeof(AbpEventBusRabbitMqModule)
    )]
public class CAServerHttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IHttpClientIpResolver, HttpClientIpResolver>();
        context.Services.AddTransient<IVerificationRequestRiskControlService, VerificationRequestRiskControlService>();
        context.Services.AddTransient<IVerificationRequestOperationDispatcher, VerificationRequestOperationDispatcher>();
        context.Services.AddTransient<IVerificationRequestOperationHandler, CreateCaHolderVerificationRequestHandler>();
        context.Services.AddTransient<IVerificationRequestOperationHandler, SocialRecoveryVerificationRequestHandler>();
        ConfigureLocalization();
    }

    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<CAServerResource>()
                .AddBaseTypes(
                    typeof(AbpUiResource)
                );
        });
    }
}
