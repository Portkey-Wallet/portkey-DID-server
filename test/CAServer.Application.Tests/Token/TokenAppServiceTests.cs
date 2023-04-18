using System.Threading.Tasks;
using CAServer.Grain.Tests;
using CAServer.Tokens;
using Orleans;
using Shouldly;
using Xunit;

namespace CAServer.Token;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class TokenAppServiceTests : CAServerGrainOrleansTestBase
{
    private readonly IClusterClient _clusterClient;
    public TokenAppServiceTests()
    {
        _clusterClient = GetRequiredService<IClusterClient>();
    }

    // [Fact]
    // public async Task UpdateTokenPrice_Test()
    // {
    //     await _tokenAppService.InitialToken();
    //     await _tokenAppService.UpdateTokenPriceUsdAsync();
    //     var tokenList = await _tokenAppService.GetTokenListAsync(new GetTokenListInput
    //     {
    //         Page = 1,
    //         Size = 4
    //     });
    //     tokenList.TotalCount.ShouldBe(4);
    //     tokenList.Items[0].Token.PriceInUsd.ShouldBeGreaterThan(0);
    //     tokenList.Items[1].Token.PriceInUsd.ShouldBe(0);
    // }
}