using CAServer.CAActivity.Provider;
using CAServer.CoinGeckoApi;
using CAServer.Grains;
using CAServer.Orleans.TestBase;
using CAServer.Tokens;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.RabbitMQ;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace CAServer;

[DependsOn(
    typeof(CAServerApplicationModule),
    typeof(CAServerDomainTestModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(CAServerOrleansTestBaseModule),
    typeof(CAServerDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(CAServerCoinGeckoApiModule)
)]
public class CAServerApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpRabbitMqOptions>(options =>
        {
            options.Connections.Default.UserName = "guest";
            options.Connections.Default.Password = "guest";
            options.Connections.Default.HostName = "192.168.66.135";
            options.Connections.Default.Port = 5672;
        });

        Configure<AbpRabbitMqEventBusOptions>(options =>
        {
            options.ClientName = "AElf-CAClient001";
            options.ExchangeName = "CA001";
        });
        context.Services.AddSingleton(new GraphQLHttpClient("http://192.168.66.87:8083/AElfIndexer_DApp/PortKeyIndexerCASchema/graphql", new NewtonsoftJsonSerializer()));
        context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());
        context.Services.AddSingleton<ITokenAppService, TokenAppService>();
        context.Services.AddSingleton<IActivityProvider, ActivityProvider>();
        base.ConfigureServices(context);
    }
}