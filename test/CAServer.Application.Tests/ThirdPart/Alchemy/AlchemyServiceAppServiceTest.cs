using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CAServer.ThirdPart.Alchemy;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class AlchemyServiceAppServiceTest : CAServerApplicationTestBase
{
    private readonly IAlchemyServiceAppService _alchemyServiceAppService;

    public AlchemyServiceAppServiceTest()
    {
        _alchemyServiceAppService = GetRequiredService<IAlchemyServiceAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(getMockThirdPartOptions());
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
        var result = await _alchemyServiceAppService.GetAlchemyFiatListAsync();
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
        // v1 test
        var input = new GetAlchemySignatureDto
        {
            Address = "address",
        };
        var result = await _alchemyServiceAppService.GetAlchemySignatureAsync(input);
        result.Success.ShouldBe("Success");
   
        // use object        
        result = _alchemyServiceAppService.GetAlchemySignatureV2Async( new AlchemyOrderUpdateDto
            {
                MerchantOrderNo = "00000000-0000-0000-0000-000000000000",
                Address = "00000000-0000-0000-0000-000000000000",
                Status = "2",
                Signature = "aaabbb"
                
            });
        result.Success.ShouldBe("Success");

        // sign value
        result = _alchemyServiceAppService.GetAlchemySignatureV2Async(new Dictionary<string, string>()
        {
            ["orderId"] = "1400006666",
            ["network"] = "TRX",
            ["amount"] = "1234",
            ["cryptoCurrency"] = "USDT",
            ["fiat"] = "USD",
            ["type"] = "ONE",
            ["timestamp"] = "123",
            ["appid"] = "12344fdsfdsfdsfsdfdsfsdfsdfdsfsdfa"
        });
        result.Signature.ShouldBe("cc9b2e7df4f875f387d0532c7bd7fbf718355576fedaae70bda52f2f84e9732e");
    }

}