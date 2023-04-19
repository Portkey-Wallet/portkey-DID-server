using CAServer.Grains.Grain.Contacts;
using Newtonsoft.Json.Serialization;
using Shouldly;
using Xunit;

namespace CAServer.Grain.Tests.CAAccount;

[Collection(ClusterCollection.Name)]
public class CaHolderTest : CAServerGrainTestBase
{
    private const string DefaultCaHash = "DefaultChainName";
    private const string DefaultNickname = "DefaultEndPoint";

    [Fact]
    public async Task AddHolderTest()
    {
        var dto = new CAHolderGrainDto
        {
            UserId = Guid.NewGuid(),
            CaHash = DefaultCaHash,
            Nickname = DefaultNickname
        };
        
        var grain = Cluster.Client.GetGrain<ICAHolderGrain>(Guid.NewGuid());
        var result = await grain.AddHolderAsync(dto);
        result.Success.ShouldBeTrue();
        result.Data.CaHash.ShouldBe(DefaultCaHash);
    }
}