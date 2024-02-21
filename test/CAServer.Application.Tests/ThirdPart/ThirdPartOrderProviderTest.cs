using System;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
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
    private async Task AddOrder()
    {
        
    }
    
    
    [Fact]
    public async Task GetThirdPartOrdersByPageAsyncTest()
    {
        var result = await _orderProvider.GetThirdPartOrdersByPageAsync(new GetThirdPartOrderConditionDto(0, 10));
        result.TotalCount.ShouldBe(0);
    }
}