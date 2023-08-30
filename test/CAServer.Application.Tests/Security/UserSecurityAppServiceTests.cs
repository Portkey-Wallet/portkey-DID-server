using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using CAServer.Security.Dtos;
using Microsoft.Extensions.DependencyInjection;
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
        data.DailyLimit.ShouldBe(10000);
        data.SingleLimit.ShouldBe(10000);
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
}