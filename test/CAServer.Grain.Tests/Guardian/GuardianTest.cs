using CAServer.Grains.Grain;
using CAServer.Grains.Grain.Guardian;
using Shouldly;
using Xunit;

namespace CAServer.Grain.Tests.Guardian;

[Collection(ClusterCollection.Name)]
public class GuardianTest : CAServerGrainTestBase
{
    [Fact]
    public async Task AddGuardianTest()
    {
        var identifier = "test@qq.com";
        var salt = "salt";
        var identifierHash = "identifierHash";

        var grain = Cluster.Client.GetGrain<IGuardianGrain>(Guid.NewGuid().ToString());
        var result = await grain.AddGuardianAsync(identifier, salt, identifierHash);
        result.Success.ShouldBeTrue();
        result.Data.Identifier.ShouldBe(identifier);
    }

    [Fact]
    public async Task AddGuardian_Twice_Test()
    {
        var identifier = "test@qq.com";
        var salt = "salt";
        var identifierHash = "identifierHash";

        var grain = Cluster.Client.GetGrain<IGuardianGrain>(Guid.NewGuid().ToString());
        await grain.AddGuardianAsync(identifier, salt, identifierHash);

        var result = await grain.AddGuardianAsync(identifier, salt, identifierHash);
        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Guardian hash info has already exist.");
    }

    [Fact]
    public async Task GetGuardianTest()
    {
        var identifier = "test@qq.com";
        var salt = "salt";
        var identifierHash = "identifierHash";

        var grain = Cluster.Client.GetGrain<IGuardianGrain>(Guid.NewGuid().ToString());
        await grain.AddGuardianAsync(identifier, salt, identifierHash);
        var result = await grain.GetGuardianAsync(identifier);

        result.Success.ShouldBeTrue();
        result.Data.Identifier.ShouldBe(identifier);
    }

    [Fact]
    public async Task GetGuardian_Not_Exist_Test()
    {
        var identifier = "test@qq.com";

        var grain = Cluster.Client.GetGrain<IGuardianGrain>(Guid.NewGuid().ToString());
        var result = await grain.GetGuardianAsync(identifier);

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Guardian not exist.");
    }

    [Fact]
    public async Task DeleteGuardianTest()
    {
        var identifier = "test@qq.com";
        var salt = "salt";
        var identifierHash = "identifierHash";

        var grain = Cluster.Client.GetGrain<IGuardianGrain>(Guid.NewGuid().ToString());
        await grain.AddGuardianAsync(identifier, salt, identifierHash);
        var result = await grain.GetGuardianAsync(identifier);
        result.Success.ShouldBeTrue();
        result.Data.Identifier.ShouldBe(identifier);

        var deleteResult = await grain.DeleteGuardian();
        deleteResult.Success.ShouldBeTrue();
        deleteResult.Data.Identifier.ShouldBe(identifier);
    }
    
    [Fact]
    public async Task DeleteNotExistGuardianTest()
    {
        var grain = Cluster.Client.GetGrain<IGuardianGrain>(Guid.NewGuid().ToString());

        var deleteResult = await grain.DeleteGuardian();
        deleteResult.Success.ShouldBeFalse();
        deleteResult.Message.ShouldBe("Guardian not exist.");
    }
    
}