using CAServer.CoinGeckoApi;
using CAServer.EntityEventHandler.Core;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectMapping;

namespace CAServer.Orleans.TestBase;


[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpAuthorizationModule),
    typeof(CAServerDomainModule),
    typeof(CAServerCoinGeckoApiModule),
    typeof(AbpCachingModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpObjectMappingModule),
    typeof(CAServerEntityEventHandlerCoreModule)
)]
public class CAServerOrleansTestBaseModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        context.Services.Configure<CoinGeckoOptions>(o =>
        {
            o.CoinIdMapping["ELF"] = "aelf";
        });
    }
}