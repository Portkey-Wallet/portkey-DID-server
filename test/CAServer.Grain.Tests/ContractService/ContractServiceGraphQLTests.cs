using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.CrossChain;
using CAServer.Grains.State.CrossChain;
using Shouldly;
using Xunit;

namespace CAServer.Grain.Tests.ContractService;

public class ContractServiceGraphQLTests : CAServerGrainTestBase
{
    [Fact]
    public async Task GraphQLTests()
    {
        var grain = Cluster.Client.GetGrain<IContractServiceGraphQLGrain>("GrainName");

        await grain.SetStateAsync(1000);
        var height = await grain.GetStateAsync();
        
        height.ShouldBe(1000);
    }
}