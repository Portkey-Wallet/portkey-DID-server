using CAServer.Grains.Grain.ValidateMerkerTree;
using CAServer.ValidateMerkerTree;
using Shouldly;
using Xunit;

namespace CAServer.Grain.Tests.ValidateMerkerTree;

[Collection(ClusterCollection.Name)]
public class ValidateMerkerTreeTest : CAServerGrainTestBase
{
    
    [Fact]
    public async Task StatusTest()
    {
        var id = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IValidateOriginChainIdGrain>(id);
        var dto = await grain.GetInfoAsync();
        dto.Data.Status.ShouldBe(ValidateStatus.Init);

        var result = await grain.NeedValidateAsync();
        result.Data.ShouldBeTrue();
        dto = await grain.GetInfoAsync();
        dto.Data.Status.ShouldBe(ValidateStatus.Processing);
        
        result = await grain.NeedValidateAsync();
        result.Data.ShouldBeFalse();
        
        await grain.SetInfoAsync("111", "222","tDVW");
        dto = await grain.GetInfoAsync();
        dto.Data.Status.ShouldBe(ValidateStatus.Processing);
        result = await grain.NeedValidateAsync();
        result.Data.ShouldBeFalse();

        await grain.SetStatusSuccessAsync();
        result = await grain.NeedValidateAsync();
        result.Data.ShouldBeFalse();
        
        await grain.SetStatusFailAsync();
        result = await grain.NeedValidateAsync();
        result.Data.ShouldBeFalse();
    }
}