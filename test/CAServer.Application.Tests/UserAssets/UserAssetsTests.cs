using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.UserAssets.Provider;
using Shouldly;
using Xunit;

namespace CAServer.UserAssets;

public class UserAssetsTests : CAServerApplicationOrleansTestBase
{
    private readonly IUserAssetsProvider _userAssetsProvider;
    private readonly IUserAssetsAppService _userAssetsAppService;

    public UserAssetsTests()
    {
        _userAssetsAppService = GetRequiredService<UserAssetsAppService>();
        _userAssetsProvider = GetRequiredService<UserAssetsProvider>();
    }

    [Fact]
    public async Task Test()
    {
        var request = new GetTokenRequestDto
        {
            CaAddresses = new List<string> { "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo" },
            SkipCount = 0,
            MaxResultCount = 10
        };
        var result = await _userAssetsAppService.GetTokenAsync(request);
        
    }

    [Fact]
    public async Task GetTokenTest()
    {
        var list = new List<string> { "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo" };

        var result = await _userAssetsProvider.GetUserTokenInfoAsync(list, "", 0, 10);
        result.CaHolderTokenBalanceInfo.Data.Last().ChainId.ShouldContain("AELF");
    }

    [Fact]
    public async Task GetNFTProtocolsTest()
    {
        var list = new List<string> { "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo" };

        var result = await _userAssetsProvider.GetUserTokenInfoAsync(list, "", 0, 10);
        result.CaHolderTokenBalanceInfo.Data.Last().ChainId.ShouldContain("AELF");
    }

    [Fact]
    public async Task GetNftInfosTest()
    {
        var list = new List<string> { "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo" };

        var result = await _userAssetsProvider.GetUserTokenInfoAsync(list, "", 0, 10);
        result.CaHolderTokenBalanceInfo.Data.Last().ChainId.ShouldContain("AELF");
    }

    [Fact]
    public async Task GetRecentTransactionUsersTest()
    {
        var list = new List<string> { "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo" };

        var result = await _userAssetsProvider.GetRecentTransactionUsersAsync(list, 0, 10);
        result.CaHolderTransactionAddressInfo.Data.Last().ChainId.ShouldBe("AELF");
    }

    [Fact]
    public async Task SearchUserAssetsTest()
    {
        var list = new List<string> { "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo" };

        var result = await _userAssetsProvider.SearchUserAssetsAsync(list, null, 0, 10);
        result.CaHolderSearchTokenNFT.Data.Last().ChainId.ShouldContain("AELF");
    }
}