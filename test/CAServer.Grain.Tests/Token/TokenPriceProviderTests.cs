using CAServer.Grains.Grain.Tokens;
using CAServer.Grains.Grain;
using Shouldly;
using Volo.Abp.Caching;
using Xunit;

namespace CAServer.Grain.Tests.Token;

public class TokenPriceProviderTests : CAServerGrainOrleansTestBase
{
    private const string ELFTokenSymbol = "ELF";
    

    // [Fact]
    // public async Task GetTokenPrice_Test()
    // {
    //     var tokenPriceGrain = Cluster.Client.GetGrain<ITokenPriceProviderGrain>(0);
    //     var price = await tokenPriceGrain.GetPriceAsync(ELFTokenSymbol);
    //     price.ShouldBeGreaterThan(0);
    // }
    //
    // [Fact]
    // public async Task GetTokenHistoryPrice_Test()
    // {
    //     var time = DateTime.UtcNow;
    //     var tokenPriceGrain = Cluster.Client.GetGrain<ITokenPriceProviderGrain>(0);
    //     var price = await tokenPriceGrain.GetHistoryPriceAsync(ELFTokenSymbol,time);
    //     price.ShouldBeGreaterThan(0);
    // }
    //
    // [Fact]
    // public async Task GetTokenPrice_Test_NoPrice()
    // {
    //     var tokenPriceGrain = Cluster.Client.GetGrain<ITokenPriceProviderGrain>(0);
    //     var price = await tokenPriceGrain.GetPriceAsync("cpu");
    //     price.ShouldBe(0);
    // }
    //
    // [Fact]
    // public async Task GetTokenHistoryPrice_Test_NoPrice()
    // {
    //     var time = DateTime.UtcNow;
    //     var tokenPriceGrain = Cluster.Client.GetGrain<ITokenPriceProviderGrain>(0);
    //     var price = await tokenPriceGrain.GetHistoryPriceAsync(ELFTokenSymbol,time);
    //     price.ShouldBeGreaterThan(0);
    // }
    
}