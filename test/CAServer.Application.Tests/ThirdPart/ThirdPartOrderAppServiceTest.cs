using System;
using System.Linq;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CAServer.ThirdPart;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class ThirdPartOrderAppServiceTest : CAServerApplicationTestBase
{
    private readonly IThirdPartOrderAppService _thirdPartOrderAppService;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;


    public ThirdPartOrderAppServiceTest()
    {
        _thirdPartOrderAppService = GetRequiredService<IThirdPartOrderAppService>();
        _thirdPartOrderProvider = GetRequiredService<IThirdPartOrderProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(getMockTokenPriceGrain());
        services.AddSingleton(getMockOrderGrain());
        services.AddSingleton(getMockDistributedEventBus());
    }

    [Fact]
    public async Task GetThirdPartOrderListAsyncTest()
    {
        var result = await _thirdPartOrderAppService.GetThirdPartOrdersAsync(new GetUserOrdersDto()
        {
            SkipCount = 1,
            MaxResultCount = 10
        });
        var data = result.Data.First();
        data.Address.ShouldBe("Address");
        data.MerchantName.ShouldBe("MerchantName");
        data.Crypto.ShouldBe("Crypto");
        data.CryptoPrice.ShouldBe("CryptoPrice");
        data.Fiat.ShouldBe("Fiat");
        data.FiatAmount.ShouldBe("FiatAmount");
        data.LastModifyTime.ShouldBe("LastModifyTime");
    }

    [Fact]
    public async Task CreateThirdPartOrderAsyncTest()
    {
        var input = new CreateUserOrderDto
        {
            MerchantName = "123",
            TransDirect = "123"
        };

        var result = _thirdPartOrderAppService.CreateThirdPartOrderAsync(input);
        result.Result.Success.ShouldBe(true);
    }

    [Fact]
    public async Task GetThirdPartOrdersByPageAsyncTest()
    {
        var userId = Guid.NewGuid();
        var skipCount = 1;
        var maxResultCount = 10;

        var orderList = _thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(userId, skipCount, maxResultCount);
        orderList.Result.Count.ShouldBe(1);
    }
}