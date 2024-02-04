using System;
using CAServer.Cache;
using CAServer.Signature.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;

namespace CAServer.CoinGeckoApi;

[DependsOn(
    typeof(AbpCachingModule))]
public class CAServerCoinGeckoApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<CoinGeckoOptions>(configuration.GetSection("CoinGecko"));
        Configure<SignatureServerOptions>(configuration.GetSection("SignatureServer"));
        context.Services.AddHttpClient();
        context.Services.AddMemoryCache();
        context.Services.AddSingleton(typeof(ILocalMemoryCache<>), typeof(LocalMemoryCache<>));
    }
}