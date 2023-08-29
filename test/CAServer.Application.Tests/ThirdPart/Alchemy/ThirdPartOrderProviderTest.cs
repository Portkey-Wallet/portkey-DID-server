using System;
using System.Threading.Tasks;
using CAServer.ThirdPart.Provider;
using Shouldly;
using Xunit;

namespace CAServer.ThirdPart.Alchemy;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class ThirdPartOrderProviderTest : CAServerApplicationTestBase
{
    private readonly IThirdPartOrderProvider _orderProvider;

    public ThirdPartOrderProviderTest()
    {
        _orderProvider = GetRequiredService<IThirdPartOrderProvider>();
    }

    [Fact]
    public async Task GetThirdPartOrdersByPageAsyncTest()
    {
        var result = await _orderProvider.GetThirdPartOrdersByPageAsync(Guid.Empty, null, 0, 10);
        result.TotalRecordCount.ShouldBe(0);
    }
}