using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CAServer.ThirdPart.Alchemy;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class AlchemyServiceAppServiceTest : ThirdPartTestBase
{
    private readonly IAlchemyServiceAppService _alchemyServiceAppService;

    public AlchemyServiceAppServiceTest()
    {
        _alchemyServiceAppService = GetRequiredService<IAlchemyServiceAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockThirdPartOptions());
        services.AddSingleton(GetMockAlchemyFiatDto());
        services.AddSingleton(GetMockAlchemyOrderQuoteDto());
    }

    [Fact]
    public async Task GetAlchemyOrderQuoteAsyncTest()
    {
        var input = new GetAlchemyOrderQuoteDto()
        {
            Crypto = "USDT",
            Network = "ETH",
            Fiat = "USD",
            Country = "US",
            Amount = "201",
            Side = "SELL",
            Type = "ONE"
        };
        var result = await _alchemyServiceAppService.GetAlchemyOrderQuoteAsync(input);
        result.Success.ShouldBe("Success");
    }

    [Fact]
    public async Task GetAlchemyOrderQuoteAsync_Buy_Test()
    {
        var input = new GetAlchemyOrderQuoteDto()
        {
            Crypto = "USDT",
            Network = "ETH",
            Fiat = "USD",
            Country = "US",
            Amount = "201",
            Side = "BUY",
            Type = "ONE"
        };
        var result = await _alchemyServiceAppService.GetAlchemyOrderQuoteAsync(input);
        result.Success.ShouldBe("Success");
    }

    /**
     *
        Task<AlchemyTokenDto> GetAlchemyFreeLoginTokenAsync(GetAlchemyFreeLoginTokenDto input);
        Task<AlchemyFiatListDto> GetAlchemyFiatListAsync();
        Task<AlchemyCryptoListDto> GetAlchemyCryptoListAsync(GetAlchemyCryptoListDto input);
        Task<AlchemyOrderQuoteResultDto> GetAlchemyOrderQuoteAsync(GetAlchemyOrderQuoteDto input);
        Task<AlchemySignatureResultDto> GetAlchemySignatureAsync(GetAlchemySignatureDto input);
     */
    [Fact]
    public async Task GetAlchemyFreeLoginTokenAsyncTest()
    {
        var input = new GetAlchemyFreeLoginTokenDto
        {
            Email = "test@portkey.finance"
        };
        var result = await _alchemyServiceAppService.GetAlchemyFreeLoginTokenAsync(input);
        result.Success.ShouldBe("Success");
    }

    [Fact]
    public async Task GetAlchemyFiatListAsyncTest()
    {
        var input = new GetAlchemyFiatListDto()
        {
            Type = "BUY"
        };
        var result = await _alchemyServiceAppService.GetAlchemyFiatListAsync(input);
        result.Success.ShouldBe("Success");
    }

    [Fact]
    public async Task GetAlchemyFiatListAsync_Sell_Test()
    {
        var input = new GetAlchemyFiatListDto()
        {
            Type = "SELL"
        };
        var result = await _alchemyServiceAppService.GetAlchemyFiatListAsync(input);
        result.Success.ShouldBe("Success");
    }

    [Fact]
    public async Task GetAlchemyCryptoListAsyncTest()
    {
        var result = await _alchemyServiceAppService.GetAlchemyCryptoListAsync(new GetAlchemyCryptoListDto());
        result.Success.ShouldBe("Success");
    }

    [Fact]
    public async Task GetAlchemySignatureAsyncTest()
    {
        var result = await _alchemyServiceAppService.GetAlchemySignatureAsync(new GetAlchemySignatureDto()
        {
            Address = "Test"
        });
    }

    [Fact]
    public async Task GetAlchemyApiSignatureAsyncTest()
    {
        var result = await _alchemyServiceAppService.GetAlchemyApiSignatureAsync(new Dictionary<string, object>
        {
            ["orderNo"] = "994864610797428736",
            ["merchantOrderNo"] = "03da9b8e-ee3b-de07-a53d-2e3cea36b2c4",
            ["amount"] = "100",
            ["fiat"] = "USD",
            ["payTime"] = "2022-07-08 15:18:43",
            ["payType"] = "CREDIT_CARD",
            ["type"] = "MARKET",
            ["name"] = "LUCK",
            ["quantity"] = "1",
            ["uniqueId"] = "LUCK",
            ["appId"] = "test",
            ["message"] = "",
        });
        result.Data.ShouldBe("rd60vvnWPiE8LgsIY9lKbYbYBuQ=");
    }
}