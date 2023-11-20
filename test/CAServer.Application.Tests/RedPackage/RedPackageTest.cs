using System.Threading.Tasks;
using CAServer.RedPackage.Dtos;
using Shouldly;
using Xunit;

namespace CAServer.RedPackage;

public partial class RedPackageTest
{
    private readonly IRedPackageAppService _redPackageAppService;
    
    public RedPackageTest(IRedPackageAppService redPackageAppService)
    {
        _redPackageAppService = redPackageAppService;
    }
    
    [Fact]
    public async Task GenerateRedPackageAsync_test()
    {
        var res = await _redPackageAppService.GenerateRedPackageAsync(null);
        res.ChainId.ShouldBeNullOrEmpty();
        res.Symbol.ShouldBeNullOrEmpty();
        res = await _redPackageAppService.GenerateRedPackageAsync(new GenerateRedPackageInputDto()
        {
            ChainId = "AELF",
            Symbol = "ELF"
        });
        res.ChainId.ShouldBe("chainId");
        res.Symbol.ShouldBe("symbol");
    }
}