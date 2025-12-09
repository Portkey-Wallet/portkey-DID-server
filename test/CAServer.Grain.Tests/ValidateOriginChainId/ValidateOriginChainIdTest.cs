using CAServer.Grains.Grain.ValidateOriginChainId;
using CAServer.ValidateOriginChainId;
using Shouldly;
using Xunit;

namespace CAServer.Grain.Tests.ValidateOriginChainId;

[Collection(ClusterCollection.Name)]
public class ValidateOriginChainIdTest : CAServerGrainTestBase
{
    
    // [Fact]
    // public async Task StatusTest()
    // {
    //     var id = Guid.NewGuid();
    //     var grain = Cluster.Client.GetGrain<IValidateOriginChainIdGrain>(id);
    //     // var dto = await grain.GetInfoAsync();
    //     // dto.Data.Status.ShouldBe(ValidateStatus.Init);
    //
    //     var result = await grain.NeedValidateAsync();
    //     result.Data.ShouldBeTrue();
    //     var dto = await grain.GetInfoAsync();
    //     dto.Data.Status.ShouldBe(ValidateStatus.Processing);
    //     
    //     result = await grain.NeedValidateAsync();
    //     result.Data.ShouldBeFalse();
    //     
    //     await grain.SetInfoAsync("111", "tDVW");
    //     dto = await grain.GetInfoAsync();
    //     dto.Data.Status.ShouldBe(ValidateStatus.Processing);
    //     result = await grain.NeedValidateAsync();
    //     result.Data.ShouldBeFalse();
    //
    //     await grain.SetStatusSuccessAsync();
    //     result = await grain.NeedValidateAsync();
    //     result.Data.ShouldBeFalse();
    //     
    //     await grain.SetStatusFailAsync();
    //     result = await grain.NeedValidateAsync();
    //     result.Data.ShouldBeFalse();
    // }
}