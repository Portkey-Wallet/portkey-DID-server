using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using CAServer.Options;
using CAServer.Security.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Xunit;

namespace CAServer.Security;

public partial class UserSecurityAppServiceTest : CAServerApplicationTestBase
{
    private readonly string defaultSymbol = "ELF";
    private readonly string defaultChainId = "AELF";
    private readonly string defaultTestCaHash = HashHelper.ComputeFrom("test@google.com").ToHex();

    protected IUserSecurityAppService _userSecurityAppService;

    public UserSecurityAppServiceTest()
    {
        _userSecurityAppService = GetRequiredService<IUserSecurityAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockUserAssetsProvider());
        services.AddSingleton(GetMockUserSecurityProvider());
        services.AddSingleton(GetContractProvider());
        services.AddSingleton(MockSecurityOptions());
    }

    [Fact]
    public async Task GetTransferLimitListByCaHashAsyncTest()
    {
        var result = await _userSecurityAppService.GetTransferLimitListByCaHashAsync(
            new GetTransferLimitListByCaHashDto
            {
                MaxResultCount = 100,
                SkipCount = 0,
                CaHash = defaultTestCaHash
            });
        result.TotalRecordCount.ShouldBe(1);

        var data = result.Data.First();
        data.Symbol.ShouldBe(defaultSymbol);
        data.ChainId.ShouldBe(defaultChainId);
        data.DailyLimit.ShouldBe("10000");
        data.SingleLimit.ShouldBe("10000");
    }

    [Fact]
    public async Task GetManagerApprovedListByCaHashAsyncTest()
    {
        var result = await _userSecurityAppService.GetManagerApprovedListByCaHashAsync(
            new GetManagerApprovedListByCaHashDto()
            {
                MaxResultCount = 100,
                SkipCount = 0,
                ChainId = defaultChainId,
                CaHash = defaultTestCaHash
            });
        result.TotalRecordCount.ShouldBe(1);

        var data = result.Data.First();
        data.Symbol.ShouldBe(defaultSymbol);
        data.ChainId.ShouldBe(defaultChainId);
        data.Amount.ShouldBe(10000);
        data.CaHash.ShouldBe(defaultTestCaHash);
        data.Spender.ShouldBe(Address.FromPublicKey("AAA".HexToByteArray()).ToBase58());
    }

    private IOptionsSnapshot<SecurityOptions> MockSecurityOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<SecurityOptions>>();
        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new SecurityOptions
            {
                DefaultTokenTransferLimit = 1000,
                TokenTransferLimitDict = new Dictionary<string, TokenTransferLimit>()
                {
                    {
                        "AELF", new TokenTransferLimit()
                        {
                            SingleTransferLimit = new Dictionary<string, string>() { ["ELF"] = "20000000000" },
                            DailyTransferLimit = new Dictionary<string, string>() { ["ELF"] = "20000000000" }
                        }
                    }
                },
                TokenBalanceTransferThreshold = new Dictionary<string, long>() { { "ELF", 100000000000 } }
            });
        return mockOptionsSnapshot.Object;
    }
}