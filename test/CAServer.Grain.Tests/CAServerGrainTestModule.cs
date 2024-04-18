using CAServer.CoinGeckoApi;
using CAServer.Grains;
using CAServer.Grains.Grain.Account;
using CAServer.Signature;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.Auditing;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grain.Tests;

[DependsOn(
    typeof(CAServerGrainsModule),
    typeof(CAServerDomainTestModule),
    typeof(CAServerDomainModule),
    typeof(CAServerCoinGeckoApiModule),
    typeof(AbpCachingModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpObjectMappingModule),
    typeof(CAServerSignatureModule)
)]
public class CAServerGrainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAuditingOptions>(options =>
        {
            options.IsEnabled = false;
        });
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        context.Services.AddHttpClient();
        //context.Services.Configure<CoinGeckoOptions>(o => { o.CoinIdMapping["ELF"] = "aelf"; });
        context.Services.Configure<CAAccountOption>(o =>
        {
            o.CAAccountRequestInfoMaxLength = 100;
            o.CAAccountRequestInfoExpirationTime = 1;
        });
    }
}