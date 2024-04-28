using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace CAServer.ThirdPart.Alchemy;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class AlchemyServiceAppServiceTest : ThirdPartTestBase
{
    private readonly IAlchemyServiceAppService _alchemyServiceAppService;
    private readonly ITestOutputHelper _testOutputHelper;

    public AlchemyServiceAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _alchemyServiceAppService = GetRequiredService<IAlchemyServiceAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        var mockOptions = MockThirdPartOptions();
        services.AddSingleton(mockOptions);
        services.AddSingleton(MockRampOptions());
        services.AddSingleton(MockSecretProvider());

        // mock http
        services.AddSingleton(MockHttpFactory(_testOutputHelper,
            
            PathMatcher(HttpMethod.Get, AlchemyApi.QueryNftFiatList.Path,
                new AlchemyBaseResponseDto<List<AlchemyFiatDto>>(
                    new() { new AlchemyFiatDto { Country = "US", Currency = "USD"} })),
            
            PathMatcher(HttpMethod.Get, AlchemyApi.QueryFiatList.Path,
                new AlchemyBaseResponseDto<List<AlchemyFiatDto>>(
                    new() { new AlchemyFiatDto { Country = "US", Currency = "USD", PayMax = "2000", PayMin = "20"} })),
            
            PathMatcher(HttpMethod.Get, mockOptions.CurrentValue.Alchemy.CryptoListUri,
                new AlchemyBaseResponseDto<List<AlchemyCryptoDto>>(
                    new() { new AlchemyCryptoDto { Crypto = "ELF", Network = "ELF", BuyEnable = "1", SellEnable = "1" } })),
            
            PathMatcher(HttpMethod.Post, mockOptions.CurrentValue.Alchemy.GetTokenUri,
                new AlchemyBaseResponseDto<AlchemyTokenDataDto>(
                    new AlchemyTokenDataDto { AccessToken = "AccessToken" })),
            
            PathMatcher(HttpMethod.Post, AlchemyApi.GetFreeLoginToken.Path,
                new AlchemyBaseResponseDto<AlchemyTokenDataDto>(
                    new AlchemyTokenDataDto { AccessToken = "AccessToken" })),
            
            PathMatcher(HttpMethod.Post, mockOptions.CurrentValue.Alchemy.OrderQuoteUri,
                new AlchemyBaseResponseDto<AlchemyOrderQuoteDataDto>(
                    new AlchemyOrderQuoteDataDto
                    {
                        Crypto = "ELF",
                        CryptoPrice = "1",
                        CryptoQuantity = "1",
                        Fiat = "USD",
                        FiatQuantity = "1",
                        RampFee = "0.2",
                        NetworkFee = "0.1",
                    }))
        ));
    }

    [Fact]
    public async Task GetAlchemyOrderQuoteAsyncTest()
    {
        var input = new GetAlchemyOrderQuoteDto()
        {
            Crypto = "ELF",
            Network = "ELF",
            Fiat = "USD",
            Country = "US",
            Amount = "201",
            Side = "SELL",
            Type = "ONE"
        };
        var result = await _alchemyServiceAppService.GetAlchemyOrderQuoteAsync(input);
        result.Success.ShouldBe(true);
    }

    [Fact]
    public async Task GetAlchemyOrderQuoteAsync_Buy_Test()
    {
        var input = new GetAlchemyOrderQuoteDto()
        {
            Crypto = "ELF",
            Network = "ELF",
            Fiat = "USD",
            Country = "US",
            Amount = "201",
            Side = "BUY",
            Type = "ONE"
        };
        
        // from mock http
        var result = await _alchemyServiceAppService.GetAlchemyOrderQuoteAsync(input);
        result.Success.ShouldBe(true);
        
        // from cache
        result = await _alchemyServiceAppService.GetAlchemyOrderQuoteAsync(input);
        result.Success.ShouldBe(true);

        input.Side = "SELL";
        result = await _alchemyServiceAppService.GetAlchemyOrderQuoteAsync(input);
        result.Success.ShouldBe(true);
    }
    
    [Fact]
    public async Task GetAlchemyFreeLoginTokenAsyncTest()
    {
        var input = new GetAlchemyFreeLoginTokenDto
        {
            Email = "test@portkey.finance"
        };
        var result = await _alchemyServiceAppService.GetAlchemyFreeLoginTokenAsync(input);
        result.Success.ShouldBe(true);
    }
    
    [Fact]
    public async Task GetAlchemyNftFreeLoginTokenAsyncTest()
    {
        var input = new GetAlchemyFreeLoginTokenDto
        {
            Email = "test@portkey.finance"
        };
        var result = await _alchemyServiceAppService.GetAlchemyNftFreeLoginTokenAsync(input);
        result.Success.ShouldBe(true);
    }

    [Fact]
    public async Task GetAlchemyFiatListAsyncTest()
    {
        var input = new GetAlchemyFiatListDto()
        {
            Type = "BUY"
        };
        var result = await _alchemyServiceAppService.GetAlchemyFiatListWithCacheAsync(input);
        result.Success.ShouldBe(true);
    }

    [Fact]
    public async Task GetAlchemyFiatListAsync_Sell_Test()
    {
        var input = new GetAlchemyFiatListDto()
        {
            Type = "SELL"
        };
        var result = await _alchemyServiceAppService.GetAlchemyFiatListWithCacheAsync(input);
        result.Success.ShouldBe(true);
    }

    [Fact]
    public async Task GetAlchemyCryptoListAsyncTest()
    {
        var result = await _alchemyServiceAppService.GetAlchemyCryptoListAsync(new GetAlchemyCryptoListDto());
        result.Success.ShouldBe(true);
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
        var result = await _alchemyServiceAppService.GetAlchemyApiSignatureAsync(new Dictionary<string, string>
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

    [Fact]
    public async Task QueryNftFiatList()
    {
        var list = await _alchemyServiceAppService.GetAlchemyNftFiatListAsync();
        list.ShouldNotBeEmpty();
        list[0].Country.ShouldBe("US");
    }
}