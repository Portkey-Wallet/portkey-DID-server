using CAServer;
using Microsoft.Extensions.DependencyInjection;
using MockServer.Options;
using Volo.Abp.Account;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.HttpApi;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace MockServer;
[DependsOn(
    typeof(CAServerApplicationContractsModule),
    // typeof(CAServerDomainModule),
    typeof(AbpAccountHttpApiModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(AbpPermissionManagementHttpApiModule),
    typeof(AbpTenantManagementHttpApiModule),
    typeof(AbpFeatureManagementHttpApiModule),
    typeof(AbpSettingManagementHttpApiModule),
    typeof(AbpEventBusRabbitMqModule)
)]
public class CAServerMockServerHttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CAServerMockServerHttpApiModule>(); });
        var configuration = context.Services.GetConfiguration();
        Configure<AlchemyOptions>(configuration.GetSection("AlchemyOptions"));
        context.Services.AddHttpClient();
    }
}