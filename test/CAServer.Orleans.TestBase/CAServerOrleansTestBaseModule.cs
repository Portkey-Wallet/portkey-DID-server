using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace CAServer.Orleans.TestBase;


[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpAuthorizationModule),
    typeof(CAServerDomainModule)
)]
public class CAServerOrleansTestBaseModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        // context.Services.Configure<VerifierAccount>(o =>
        // {
        //     o.PrivateKey = "09da44778f8db2e602fb484334f37df19e221c84c4582ce5b7770ccfbc3ddbef";
        //     o.Address = "EdbSRh8dxa5CpAZWEcWegw4X6zyv9yoTuufiPCWCjnkHH428p";
        // });
        // context.Services.AddTransient<ICoinsClient, CoinsClient>(sp => sp.GetService<CoinsClient>());
        // context.Services.AddSingleton<IRequestLimitProvider, RequestLimitProvider>(sp=>sp.GetService<RequestLimitProvider>());
        // context.Services.Configure<CoinGeckoOptions>(o =>
        // {
        //     o.CoinIdMapping["ELF"] = "aelf";
        // });
        context.Services.AddDistributedMemoryCache();
    }
}