using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace CAServer.ThirdPart.Alchemy;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class AlchemyServiceAppServiceTest : CAServerApplicationTestBase
{
    
    private readonly IAlchemyServiceAppService _alchemyServiceAppService;
    private readonly AlchemyProvider _alchemyProvider;
    private readonly ITestOutputHelper _testOutputHelper;

    public AlchemyServiceAppServiceTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _alchemyServiceAppService = GetRequiredService<IAlchemyServiceAppService>();
        _alchemyProvider = GetRequiredService<AlchemyProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(getMockThirdPartOptions());
        services.AddSingleton(MockHttpFactory(_testOutputHelper, 
            MockAlchemyFiatListResponse, 
            MockAlchemyOrderQuoteList, 
            MockGetCryptoList));
    }

    [Fact]
    public async Task GetAlchemyHeader()
    {
        _testOutputHelper.WriteLine(JsonConvert.SerializeObject(_alchemyProvider.GetAlchemyRequestHeader()));
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
}