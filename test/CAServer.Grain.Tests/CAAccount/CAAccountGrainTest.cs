using CAServer.Grains.Grain.Account;
using Shouldly;
using Xunit;

namespace CAServer.Grain.Tests.CAAccount;

[Collection(ClusterCollection.Name)]
public class CAAccountGrainTest : CAServerGrainTestBase
{
    [Fact]
    public async Task RegisterUpdateTest()
    {
        var grainId = "register";
        var reqId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IRegisterGrain>(grainId);
        await grain.RequestAsync(new RegisterGrainDto()
        {
            GrainId = grainId,
            CaHash = "test",
            Id = reqId
        });

        var updateResult = await grain.UpdateRegisterResultAsync(new CreateHolderResultGrainDto()
        {
            GrainId = grainId,
            RegisteredTime = DateTime.Now,
            Id = reqId
        });

        updateResult.Success.ShouldBeTrue();
        updateResult.Data.Id.ShouldBe(reqId);
    }

    [Fact]
    public async Task RecoveryUpdateTest()
    {
        var grainId = "recovery";
        var reqId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<IRecoveryGrain>(grainId);
        await grain.RequestAsync(new RecoveryGrainDto()
        {
            GrainId = grainId,
            CaHash = "test",
            Id = reqId
        });

        var updateResult = await grain.UpdateRecoveryResultAsync(new SocialRecoveryResultGrainDto()
        {
            GrainId = grainId,
            RecoveryTime = DateTime.Now,
            Id = reqId
        });

        updateResult.Success.ShouldBeTrue();
        updateResult.Data.Id.ShouldBe(reqId);
    }
}