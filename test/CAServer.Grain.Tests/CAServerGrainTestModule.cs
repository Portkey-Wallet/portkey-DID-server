using CAServer.CoinGeckoApi;
using CAServer.Grains;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.AutoMapper;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Caching;
using Volo.Abp.Data;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Reflection;
using Volo.Abp.Threading;

namespace CAServer.Grain.Tests;

[DependsOn(
    typeof(CAServerGrainsModule),
    typeof(CAServerDomainTestModule),
    typeof(CAServerDomainModule),
    typeof(CAServerCoinGeckoApiModule),
    typeof(AbpCachingModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpObjectMappingModule)
)]
public class CAServerGrainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        context.Services.Configure<CoinGeckoOptions>(o => { o.CoinIdMapping["ELF"] = "aelf"; });
    }
}