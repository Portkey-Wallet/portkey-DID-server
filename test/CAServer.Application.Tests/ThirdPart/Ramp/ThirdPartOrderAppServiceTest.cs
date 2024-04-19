using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AElf;
using AElf.Client.MultiToken;
using AElf.Types;
using CAServer.Commons;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using CAServer.Tokens.Provider;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Validation;
using Xunit;
using Xunit.Abstractions;

namespace CAServer.ThirdPart.Ramp;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class ThirdPartOrderAppServiceTest : ThirdPartTestBase
{
    private readonly IThirdPartOrderAppService _thirdPartOrderAppService;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly ITestOutputHelper _testOutputHelper;


    public ThirdPartOrderAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _thirdPartOrderAppService = GetRequiredService<IThirdPartOrderAppService>();
        _thirdPartOrderProvider = GetRequiredService<IThirdPartOrderProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockThirdPartOptions());
        services.AddSingleton(MockSecretProvider());
        services.AddSingleton(MockMassTransitIBus());
        services.AddSingleton(MockRampOptions());
        services.AddSingleton(MockExchangeOptions());
        services.AddSingleton(MockContractProvider());
        services.AddSingleton(MockActivityProviderCaHolder("2e701e62-0953-4dd3-910b-dc6cc93ccb0d"));
        services.AddSingleton(MockChainOptions());
        MockHttpByPath(HttpMethod.Get, "/api/v3/ticker/price",
            new BinanceTickerPrice { Symbol = "ELF", Price = "0.42" });
        MockHttpByPath(HttpMethod.Get, "/api/v5/market/index-candles", new OkxResponse<List<List<string>>>()
        {
            Data = new List<List<string>>()
            {
                new() { DateTime.UtcNow.ToUtcMilliSeconds().ToString(), "0.42", "0.42", "0.42", "0.42", "0" }
            }
        });
    }

    [Fact]
    public async Task GoogleCode()
    {
        var code = GoogleTfaHelper.GenerateGoogleAuthCode("authKey", "testUser", "testTitle");
        code.ShouldNotBeNull();
    }

    [Fact]
    public async Task VerifyGoogleCode()
    {
        var code = _thirdPartOrderAppService.VerifyOrderExportCode("395653");
        code.ShouldBe(false);
    }
    
    
    [Fact]
    public void DecodeManagerForwardCall()
    {
        var rawTransaction =
            "0a220a203e1f7576c33fb1f8dc90f1ffd7775691d182ce99456d12f01aedf871014c22b412220a20e28c0b6c4145f3534431326f3c6d5a4bd6006632fd7551c26c103c368855531618abf9860d220411d0e8922a124d616e61676572466f727761726443616c6c3286010a220a20ffc98c7be1a50ada7ca839da2ecd94834525bdcea392792957cc7f1b2a0c3a1e12220a202791e992a57f28e75a11f13af2c0aec8b0eb35d2f048d42eba8901c92e0378dc1a085472616e7366657222320a220a20a7376d782cdf1b1caa2f8b5f56716209045cd5720b912e8441b4404427656cb91203454c461880a0be819501220082f104411d1acf81058c6a65ba0ed78368c815add80349b8e1fd7c4e5e2655c3dbde582833a475408094b486ddd14c6ad0f9c0e01788f209c2d0a1e356792e5bff1d4c2e01";
        var transaction = Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(rawTransaction));
        var forwardCallDto = ManagerForwardCallDto<TransferInput>.Decode(transaction);
        _testOutputHelper.WriteLine(JsonConvert.SerializeObject(forwardCallDto));
        forwardCallDto.ForwardTransactionArgs.ShouldNotBeNull();

        rawTransaction =
            "0a220a20e53eff822ad4b33e8ed0356a55e5b8ea83a88afdb15bdedcf52646d8c13209c812220a202791e992a57f28e75a11f13af2c0aec8b0eb35d2f048d42eba8901c92e0378dc18b9e0890922041e26c0ba2a085472616e73666572322e0a220a2061033c0453282232747683ffa571455f5511b5274f2125e2ee226b7fb2ebc9c11203454c461880c2d72f";
        transaction = Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(rawTransaction));
        var expFunc = () => Task.FromResult(ManagerForwardCallDto<TransferInput>.Decode(transaction));
        var exception = Assert.ThrowsAsync<UserFriendlyException>(expFunc);
        exception.ShouldNotBeNull();
        _testOutputHelper.WriteLine(exception.Result.Message);
        exception.Result.Message.ShouldContain("Convert rawTransaction FAILED");
    }

    [Fact]
    public async Task GetRampInfo()
    {
        var result = await _thirdPartOrderAppService.GetRampCoverageAsync();
        result.Success.ShouldBeTrue();
        result.Data.ThirdPart.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetThirdPartOrderListAsyncTest()
    {
        await CreateThirdPartOrderAsyncTest();
        var result = await _thirdPartOrderAppService.GetThirdPartOrdersAsync(new GetUserOrdersDto()
        {
            SkipCount = 0,
            MaxResultCount = 10
        });
        var data = result.Items.First();
        // data.Address.ShouldBe("Address");
        data.MerchantName.ShouldBe(ThirdPartNameType.Alchemy.ToString());
        data.TransDirect.ShouldBe(TransferDirectionType.TokenBuy.ToString());
    }

    [Fact]
    public async Task<OrderCreatedDto> CreateThirdPartOrderAsyncTest()
    {
        var input = new CreateUserOrderDto
        {
            MerchantName = ThirdPartNameType.Alchemy.ToString(),
            TransDirect = TransferDirectionType.TokenBuy.ToString()
        };

        var result = await _thirdPartOrderAppService.CreateThirdPartOrderAsync(input);
        result.Success.ShouldBe(true);
        result.Id.ShouldNotBeEmpty();

        return result;
    }

    [Fact]
    public async Task CreateThirdPartOrderAsyncTest_invalidParam()
    {
        var result = await Assert.ThrowsAsync<AbpValidationException>(() =>
            _thirdPartOrderAppService.CreateThirdPartOrderAsync(new CreateUserOrderDto()));
        result.ShouldNotBeNull();
        result.Message.ShouldContain("arguments are not valid");

        result = await Assert.ThrowsAsync<AbpValidationException>(() =>
            _thirdPartOrderAppService.CreateThirdPartOrderAsync(new CreateUserOrderDto
            {
                MerchantName = "111",
                TransDirect = TransferDirectionType.TokenBuy.ToString()
            }));
        result.ShouldNotBeNull();
        result.Message.ShouldContain("arguments are not valid");
    }

    [Fact]
    public void TestGetOrderTransDirectForQuery()
    {
        var sell = AlchemyHelper.GetOrderTransDirectForQuery("sell");
        sell.ShouldBe(OrderTransDirect.SELL.ToString());
        var buy = AlchemyHelper.GetOrderTransDirectForQuery("buy");
        buy.ShouldBe(OrderTransDirect.BUY.ToString());
        var defaultVal = AlchemyHelper.GetOrderTransDirectForQuery("test");
        defaultVal.ShouldBe(OrderTransDirect.SELL.ToString());
    }
}