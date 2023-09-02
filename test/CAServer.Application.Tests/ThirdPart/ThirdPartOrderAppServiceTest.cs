using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Client.MultiToken;
using AElf.Client.Service;
using AElf.Types;
using CAServer.Common;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace CAServer.ThirdPart;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class ThirdPartOrderAppServiceTest : CAServerApplicationTestBase
{
    private readonly IThirdPartOrderAppService _thirdPartOrderAppService;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly ITestOutputHelper _testOutputHelper;


    public ThirdPartOrderAppServiceTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _thirdPartOrderAppService = GetRequiredService<IThirdPartOrderAppService>();
        _thirdPartOrderProvider = GetRequiredService<IThirdPartOrderProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(getMockTokenPriceGrain());
        services.AddSingleton(getMockOrderGrain());
        services.AddSingleton(getMockDistributedEventBus());
        services.AddSingleton(base.GetMockeActivityProvider());
    }

    [Fact]
    public async void DecodeManagerForwardCall()
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

    // [Fact]
    public async void MakeManagerForwardCal()
    {
        var tokenAddress = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE";
        var caAddress = "2u6Dd139bHvZJdZ835XnNKL5y6cxqzV9PEWD5fZdQXdFZLgevc";

        var caHash = "ffc98c7be1a50ada7ca839da2ecd94834525bdcea392792957cc7f1b2a0c3a1e";
        var pk = "191912fcda8996fda0397daf3b0b1eee840b1592c6756a1751723f98cd54812c";
        var user = new UserWrapper(new AElfClient("http://192.168.67.18:8000"), pk);
        var orderId = "857569b8-7b73-e65d-36d3-3a0c7f872113";
        var transferRawTransaction = await user.CreateRawTransactionAsync(
            tokenAddress,
            "Transfer",
            new Dictionary<string, object>()
            {
                ["to"] = AelfAddressHelper.ToAddressObj("jj4LacoSa95nFjdFUjsGy8NFk97Ss1RLebee5QnefY7zTYQNT"),
                ["symbol"] = "ELF",
                ["amount"] = 1_0000_0000,
            });
        var transferTx =
            Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(transferRawTransaction.RawTransaction));

        var rawTransaction = await user.CreateRawTransactionAsync(
            caAddress,
            "ManagerForwardCall",
            new Dictionary<string, object>
            {
                ["ca_hash"] = new HashObj(caHash),
                ["contract_address"] = AelfAddressHelper.ToAddressObj(tokenAddress),
                ["method_name"] = "Transfer",
                ["args"] = UserWrapper.StringToByteArray(transferTx.Params.ToHex())
            });

        var transaction =
            Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(rawTransaction.RawTransaction));
        transaction.Signature = user.GetSignatureWith(transaction.GetHash().ToByteArray());
        var rawTransactionHex = transaction.ToByteArray().ToHex();

        var data = orderId + rawTransactionHex;
        var md5 = EncryptionHelper.MD5Encrypt32(data);
        var text = Encoding.UTF8.GetBytes(md5).ComputeHash();
        var sign = user.GetSignatureWith(text).ToHex();

        _testOutputHelper.WriteLine(JsonConvert.SerializeObject(new Dictionary<string, object>()
        {
            ["merchantName"] = "Alchemy",
            ["orderId"] = orderId,
            ["rawTransaction"] = rawTransactionHex,
            ["publicKey"] = user.PublicKey,
            ["signature"] = sign
        }));
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

        var orderList = await _thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(new GetThirdPartOrderConditionDto( skipCount, maxResultCount)
        {
            UserId = userId
        });
        orderList.TotalRecordCount.ShouldBe(1);
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