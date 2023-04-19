using CAServer.Grains;
using CAServer.Grains.Grain.Tokens.TokenPrice;
using Shouldly;
using Xunit;

namespace CAServer.Grain.Tests.Token;

[Collection(ClusterCollection.Name)]
public class TokenPriceGrainTests : CAServerGrainTestBase
{
    private const string ELFTokenSymbol = "ELF";

    // [Fact]
    // public async Task GetTokenPriceTest()
    // {
    //     var grain = Cluster.Client.GetGrain<ITokenPriceGrain>(ELFTokenSymbol);
    //     var result = await grain.GetCurrentPriceAsync(ELFTokenSymbol);
    //     result.Success.ShouldBeTrue();
    //     result.Data.PriceInUsd.ShouldBeGreaterThan(0);
    // }
    
    // [Fact]
    // public async Task GetTokenHistoryPriceTest()
    // {
    //     var time = DateTime.UtcNow.ToString("dd-MM-yyyy");
    //     var grainId = GrainIdHelper.GenerateGrainId("ELF", time);
    //     var grain = Cluster.Client.GetGrain<ITokenPriceSnapshotGrain>(grainId);
    //     var result = await grain.GetHistoryPriceAsync(ELFTokenSymbol,time);
    //     result.Success.ShouldBeTrue();
    //     result.Data.PriceInUsd.ShouldBeGreaterThan(0);
    // }

}