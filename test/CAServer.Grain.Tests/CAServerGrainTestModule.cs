using CAServer.Grains;
using CAServer.Grains.Grain;
using CAServer.Orleans.TestBase;
using CAServer.Grains.Grain.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;

namespace CAServer.Grain.Tests;

[DependsOn(
    typeof(CAServerGrainsModule),
    // typeof(CAServerCoinGeckoApiModule),
    // typeof(AbpCachingModule),
    typeof(CAServerTestBaseModule),
    typeof(CAServerDomainTestModule),
    typeof(CAServerOrleansTestBaseModule)
)]
public class CAServerGrainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        // context.Services.AddSingleton<ITokenGrain, TokenGrain>(sp=>sp.GetService<TokenGrain>());
        // context.Services.AddSingleton<IRequestLimitProvider, RequestLimitProvider>(sp=>sp.GetService<RequestLimitProvider>());
        // context.Services.Configure<CoinGeckoOptions>(o =>
        // {
        //     o.CoinIdMapping["ELF"] = "aelf";
        // });
        context.Services.AddDistributedMemoryCache();
    }
}