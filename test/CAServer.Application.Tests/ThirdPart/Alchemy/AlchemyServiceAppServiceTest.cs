using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute.ExceptionExtensions;
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
        
        // v2 test
        input = new GetAlchemySignatureDto
        {
            Address = "address",
            SignParams = new Dictionary<string, string>()
            {
                ["b"]="bbb",
                ["A"]="AAA",
                ["C"]="CCC",
                ["Z"]="ZZZ",
                ["a"]="aaa",
            }
        };
        result = await _alchemyServiceAppService.GetAlchemySignatureAsync(input);
        result.Success.ShouldBe("Success");

        // require params
        result = await _alchemyServiceAppService.GetAlchemySignatureAsync(new GetAlchemySignatureDto
        {
        });
        result.Success.ShouldBe("Fail");
        
    }
    
}