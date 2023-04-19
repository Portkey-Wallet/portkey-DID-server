using CAServer.Chain;
using CAServer.Grains.Grain.Account;
using CAServer.Grains.Grain.Chain;
using Shouldly;
using Xunit;

namespace CAServer.Grain.Tests.ChainInfo;

[Collection(ClusterCollection.Name)]
public class ChainInfoTest : CAServerGrainTestBase
{
    private const string DefaultChainId = "AELF";
    private const string DefaultChainName = "DefaultChainName";
    private const string DefaultEndPoint = "DefaultEndPoint";
    private const string DefaultExplorerUrl = "DefaultExplorerUrl";
    private const string DefaultCaContractAddress = "DefaultCaContractAddress";

    [Fact]
    public async Task AddChainInfoTest()
    {
        var dto = new ChainGrainDto
        {
            Id = "AELF",
            ChainId = DefaultChainId,
            ChainName = DefaultChainName,
            EndPoint = DefaultEndPoint,
            ExplorerUrl = DefaultExplorerUrl,
            CaContractAddress = DefaultCaContractAddress,
            DefaultToken = new DefaultToken()
        };
        
        var grain = Cluster.Client.GetGrain<IChainGrain>(dto.Id);
        var result = await grain.AddChainAsync(dto);
        result.Success.ShouldBeTrue();
        result.Data.ChainId.ShouldBe(DefaultChainId);
    }

    [Fact]
    public async Task UpdateChainInfoTest()
    {
        var dto = new ChainGrainDto
        {
            Id = "AELF",
            ChainId = DefaultChainId,
            ChainName = DefaultChainName,
            EndPoint = DefaultEndPoint,
            ExplorerUrl = DefaultExplorerUrl,
            CaContractAddress = DefaultCaContractAddress,
            DefaultToken = new DefaultToken()
        };
        
        var grain = Cluster.Client.GetGrain<IChainGrain>(dto.Id);
        await grain.AddChainAsync(dto);

        dto.ChainName = "Modified-Name";
        var resultDto = await grain.UpdateChainAsync(dto);
        
        resultDto.Success.ShouldBeTrue();
        resultDto.Data.ChainName.ShouldBe("Modified-Name");
    }
    
    [Fact]
    public async Task DeleteChainInfoTest()
    {
        var dto = new ChainGrainDto
        {
            Id = "AELF",
            ChainId = DefaultChainId,
            ChainName = DefaultChainName,
            EndPoint = DefaultEndPoint,
            ExplorerUrl = DefaultExplorerUrl,
            CaContractAddress = DefaultCaContractAddress,
            DefaultToken = new DefaultToken()
        };
        
        var grain = Cluster.Client.GetGrain<IChainGrain>(dto.Id);
        await grain.AddChainAsync(dto);
        var resultDto = await grain.DeleteChainAsync();
        
        resultDto.Success.ShouldBeTrue();
    }
}