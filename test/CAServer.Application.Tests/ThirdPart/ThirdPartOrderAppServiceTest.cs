using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Service;
using CAServer.Common;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
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
    public async void MakeManagerForwardCal()
    {
        var tokenAddress = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE";
        var caAddress = "2u6Dd139bHvZJdZ835XnNKL5y6cxqzV9PEWD5fZdQXdFZLgevc";
        var caHash = "e5c60d9e360a7e90fd3651e8e3eb1bc8eb40a37ce2f7ec478db62c784c251cce";
        var pk = "09ff2d585dd73238a64574977057e2f539a0bb9ed749c055a45acac33624d121";
        var user = new UserWrapper(new AElfClient("http://192.168.67.18:8000"), pk);
        var orderId = "eb076556-9250-74b6-647b-3a0bea35f17a";
        var transferRawTransaction = await user.CreateRawTransactionAsync( 
            tokenAddress, 
            "Transfer",
            new Dictionary<string, object>()
            {
                ["to"] = user.AddressObj(),
                ["symbol"] = "ELF",
                ["amount"] = 300_0000_0000,
            });
        
        var rawTransaction = await user.CreateRawTransactionAsync(
            "2u6Dd139bHvZJdZ835XnNKL5y6cxqzV9PEWD5fZdQXdFZLgevc",
            "ManagerForwardCall",
            new Dictionary<string, object>
            {
                ["ca_hash"] = new HashObj(caHash),
                ["contract_address"] = AelfAddressHelper.ToAddressObj(tokenAddress),
                ["method_name"] = "Transfer",
                ["args"] = UserWrapper.StringToByteArray(transferRawTransaction.RawTransaction) 
            });

        var data = orderId + rawTransaction.RawTransaction;
        var md5 = EncryptionHelper.MD5Encrypt32(data);
        var text = Encoding.UTF8.GetBytes(md5).ComputeHash();
        var sign = user.GetSignatureWith(text).ToHex();

        _testOutputHelper.WriteLine(JsonConvert.SerializeObject(new Dictionary<string, object>()
        {
            ["merchantName"] = "Alchemy",
            ["orderId"] = orderId,
            ["rawTransaction"] = rawTransaction.RawTransaction,
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