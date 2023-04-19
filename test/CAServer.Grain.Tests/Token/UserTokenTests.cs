using CAServer.Grains;
using CAServer.Grains.Grain.Tokens.UserTokens;
using Shouldly;
using Xunit;

namespace CAServer.Grain.Tests.Token;

[Collection(ClusterCollection.Name)]
public class UserTokenTests : CAServerGrainTestBase
{
    [Fact]
    public async Task<(Guid, Guid)> AddUserTokenTest()
    {
        var usertokenId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var token = new UserTokenGrainDto
        {
            UserId = userId,
            IsDefault = true,
            IsDisplay = true,
            Token = new Tokens.Dtos.Token
            {
                Id = Guid.NewGuid(),
                Symbol = "ELF",
                ChainId = "AELF",
                Address = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
                Decimals = 8
            }
        };
        var grain = Cluster.Client.GetGrain<IUserTokenGrain>(usertokenId);
        var result = await grain.AddUserTokenAsync(userId, token);
        result.Success.ShouldBeTrue();
        result.Data.IsDisplay.ShouldBeTrue();
        result.Data.Token.Symbol.ShouldBe("ELF");
        return (userId, usertokenId);
    }

    [Fact]
    public async Task AddUserTokenTest_Failed_AlreadyExist()
    {
        var (userId, userTokenId) = await AddUserTokenTest();
        var token = new UserTokenGrainDto
        {
            UserId = userId,
            IsDefault = true,
            IsDisplay = true,
            Token = new Tokens.Dtos.Token
            {
                Id = Guid.NewGuid(),
                Symbol = "ELF",
                ChainId = "AELF",
                Address = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
                Decimals = 8
            }
        };
        var grain = Cluster.Client.GetGrain<IUserTokenGrain>(userTokenId);
        var result = await grain.AddUserTokenAsync(userId, token);
        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("User token already existed.");
    }

    private async Task<(Guid, Guid)> AddUserToken()
    {
        var usertokenId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var token = new UserTokenGrainDto
        {
            UserId = userId,
            IsDefault = true,
            IsDisplay = true,
            Token = new Tokens.Dtos.Token
            {
                Id = Guid.NewGuid(),
                Symbol = "CPU",
                ChainId = "AELF",
                Address = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
                Decimals = 8
            }
        };
        var grain = Cluster.Client.GetGrain<IUserTokenGrain>(usertokenId);
        var result = await grain.AddUserTokenAsync(userId, token);
        result.Success.ShouldBeTrue();
        return (userId, usertokenId);
    }

    [Fact]
    public async Task ChangeTokenDisplayTest()
    {
        {
            var (userId, userTokenId) = await AddUserTokenTest();
            var grain = Cluster.Client.GetGrain<IUserTokenGrain>(userTokenId);
            var result = await grain.ChangeTokenDisplayAsync(userId, false);
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("ELF cannot be modified to display status.");
        }
        {
            var (userId, userTokenId) = await AddUserToken();
            var grain = Cluster.Client.GetGrain<IUserTokenGrain>(userTokenId);
            var result = await grain.ChangeTokenDisplayAsync(userId, false);
            result.Success.ShouldBeTrue();
            result.Data.IsDisplay.ShouldBeFalse();
            result.Data.Token.Symbol.ShouldBe("CPU");
        }
    }

    [Fact]
    public async Task ChangeTokenDisplayTest_Failed_NotMatch()
    {
        var (userId, userTokenId) = await AddUserTokenTest();
        var grain = Cluster.Client.GetGrain<IUserTokenGrain>(userTokenId);
        userId = Guid.NewGuid();
        var result = await grain.ChangeTokenDisplayAsync(userId, false);
        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("User does not matched.");
    }

    [Fact]
    public async Task UserTokenSymbolExistTest()
    {
        var userId = Guid.NewGuid();
        var grainId = GrainIdHelper.GenerateGrainId(userId.ToString("N"), "AELF",
            "ELF");
        var grain = Cluster.Client.GetGrain<IUserTokenSymbolGrain>(grainId);
        {
            var result = await grain.IsUserTokenSymbolExistAsync("AELF", "ELF");
            result.ShouldBeFalse();
        }
        {
            var resultToAdd = await grain.AddUserTokenSymbolAsync(userId, "AELF", "ELF");
            resultToAdd.ShouldBeTrue();
            var result = await grain.IsUserTokenSymbolExistAsync("AELF", "ELF");
            result.ShouldBeTrue();
        }
    }
}