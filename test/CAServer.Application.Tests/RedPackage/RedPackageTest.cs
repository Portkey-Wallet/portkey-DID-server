using System.Threading.Tasks;
using CAServer.RedPackage.Dtos;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace CAServer.RedPackage;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class RedPackageTest : CAServerApplicationTestBase
{
    private readonly IRedPackageAppService _redPackageAppService;
    
    public RedPackageTest()
    {
        _redPackageAppService = GetRequiredService<IRedPackageAppService>();;
    }
    
    [Fact]
    public async Task GenerateRedPackageAsync_test()
    {
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _redPackageAppService.GenerateRedPackageAsync(new GenerateRedPackageInputDto()
            {
                ChainId = "xxx",
                Symbol = "xxxx"
            });
        });
        
        var res = await _redPackageAppService.GenerateRedPackageAsync(new GenerateRedPackageInputDto()
        {
            ChainId = "AELF",
            Symbol = "ELF"
        });
        res.ChainId.ShouldBe("AELF");
        res.Symbol.ShouldBe("ELF");
    }
}