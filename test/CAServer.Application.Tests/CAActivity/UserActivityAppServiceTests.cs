using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using Shouldly;
using Xunit;

namespace CAServer.CAActivity;

public class UserActivityAppServiceTests : CAServerApplicationTestBase
{
    private readonly IActivityProvider _activityProvider;

    public UserActivityAppServiceTests()
    {
        _activityProvider = GetRequiredService<IActivityProvider>();
    }

    [Fact]
    public async Task GetActivityTest()
    {
        var txId = "125e4c63c3d208ca10b27e21ddd5182a3bf29f501a6877fad3df8aecd86fe957";
        var blockHash = "32d0d8f5be0d8bb6dd3167d266558cd4b8a8c3ea9f27e7806cb555e43587455a";

        var result = await _activityProvider.GetActivityAsync(txId, blockHash);
        result.CaHolderTransaction.Data.Last().MethodName.ShouldContain("Transfer");
    }

    [Fact]
    public async Task GetActivitiesTest()
    {
        var list = new List<string> { "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo" };

        var result =
            await _activityProvider.GetActivitiesAsync(list, "AELF", "ELF", ActivityConstants.DefaultTypes, 0, 10);
        result.CaHolderTransaction.Data.Last().MethodName.ShouldContain("Transfer");
    }
}