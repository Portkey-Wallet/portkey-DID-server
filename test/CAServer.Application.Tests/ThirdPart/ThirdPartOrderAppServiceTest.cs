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
        services.AddSingleton(getMockTokenPriceGrain());
        services.AddSingleton(getMockOrderGrain());
        services.AddSingleton(getMockDistributedEventBus());
    }

    [Fact]
    public async void DecodeManagerForwardCall()
    {
        var rawTransaction = "0a220a20e53eff822ad4b33e8ed0356a55e5b8ea83a88afdb15bdedcf52646d8c13209c812220a20f9f90416670ec1a0f2d302c9474d1bc7a475cb08caa366bcca16e2f3d7e549f518eed8f9082204fbd2b4ce2a124d616e61676572466f727761726443616c6c32e2010a220a20ffc98c7be1a50ada7ca839da2ecd94834525bdcea392792957cc7f1b2a0c3a1e12220a202791e992a57f28e75a11f13af2c0aec8b0eb35d2f048d42eba8901c92e0378dc1a085472616e73666572228d010a220a20e53eff822ad4b33e8ed0356a55e5b8ea83a88afdb15bdedcf52646d8c13209c812220a202791e992a57f28e75a11f13af2c0aec8b0eb35d2f048d42eba8901c92e0378dc18eed8f9082204fbd2b4ce2a085472616e73666572322e0a220a2061033c0453282232747683ffa571455f5511b5274f2125e2ee226b7fb2ebc9c11203454c461880c2d72f82f104410ed0facd9495edf7dd9d19574f1ca0f6d774745c3ed56c370349ea175b4c8c6f486983a722a367d77b4d66f301691833647a939dc4857116e734effb46e11cd001";
        var transaction = Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(rawTransaction));
        var forwardCallDto = ManagerForwardCallDto<TransferInput>.Decode(transaction);
        forwardCallDto.ForwardTransactionArgs.ShouldNotBeNull();
        forwardCallDto.ForwardTransaction.MethodName.ShouldBe("Transfer");
    }
    
    // [Fact]
    public async void MakeManagerForwardCal()
    {
        var tokenAddress = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE";
        var caAddress = "2u6Dd139bHvZJdZ835XnNKL5y6cxqzV9PEWD5fZdQXdFZLgevc";
        
        var caHash = "ffc98c7be1a50ada7ca839da2ecd94834525bdcea392792957cc7f1b2a0c3a1e";
        var pk = "191912fcda8996fda0397daf3b0b1eee840b1592c6756a1751723f98cd54812c";
        var user = new UserWrapper(new AElfClient("http://192.168.67.18:8000"), pk);
        var orderId = "5ee4a7b7-5c41-a40b-f17d-3a0c7607f66e";
        var transferRawTransaction = await user.CreateRawTransactionAsync( 
            tokenAddress, 
            "Transfer",
            new Dictionary<string, object>()
            {
                ["to"] = AelfAddressHelper.ToAddressObj("jj4LacoSa95nFjdFUjsGy8NFk97Ss1RLebee5QnefY7zTYQNT"),
                ["symbol"] = "ELF",
                ["amount"] = 300_0000_0000,
            });
        
        var rawTransaction = await user.CreateRawTransactionAsync(
            caAddress,
            "ManagerForwardCall",
            new Dictionary<string, object>
            {
                ["ca_hash"] = new HashObj(caHash),
                ["contract_address"] = AelfAddressHelper.ToAddressObj(tokenAddress),
                ["method_name"] = "Transfer",
                ["args"] = UserWrapper.StringToByteArray(transferRawTransaction.RawTransaction) 
            });
        
        var transaction = Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(rawTransaction.RawTransaction));
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

        var orderList = _thirdPartOrderProvider.GetThirdPartOrdersByPageAsync(userId, skipCount, maxResultCount);
        orderList.Result.Count.ShouldBe(1);
    }
}