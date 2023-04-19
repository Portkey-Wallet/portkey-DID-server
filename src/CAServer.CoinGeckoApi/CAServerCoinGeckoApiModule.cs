using System;
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
        context.Services.AddHttpClient("CoinGeckoPro", client =>
        {
            client.BaseAddress = new Uri(configuration["CoinGecko:BaseUrl"]);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("x-cg-pro-api-key", configuration["CoinGecko:ApiKey"]);
        });
    }
}