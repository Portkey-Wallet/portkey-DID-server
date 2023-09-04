using System.Net.Http;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using CAServer.Commons.Dtos;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Processors;
using CAServer.ThirdPart.Provider;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace CAServer.ThirdPart.NftCheckout;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class NftOrderTest : CAServerApplicationTestBase
{
    private static readonly string MerchantName = "symbolMarket";
    private static readonly ECKeyPair MerchantAccount = CryptoHelper.FromPrivateKey(ByteArrayHelper
        .HexStringToByteArray("5945c176c4269dc2aa7daf7078bc63b952832e880da66e5f2237cdf79bc59c5f"));

    private readonly IThirdPartOrderAppService _thirdPartOrderAppService;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IThirdPartNftOrderProcessorFactory _thirdPartNftOrderProcessorFactory;

    public NftOrderTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _thirdPartNftOrderProcessorFactory = GetRequiredService<IThirdPartNftOrderProcessorFactory>();
        _thirdPartOrderAppService = GetRequiredService<IThirdPartOrderAppService>();
        _testOutputHelper.WriteLine("publicKey = " + MerchantAccount.PublicKey.ToHex());
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockRandomActivityProviderCaHolder());
        services.AddSingleton(MockThirdPartOptions());
        services.AddSingleton(MockHttpFactory(_testOutputHelper, 
            PathMatcher(HttpMethod.Post, "/myWebhook", new CommonResponseDto<Empty>()),
            PathMatcher(HttpMethod.Post, AlchemyApi.NftResultNotice.Path, new CommonResponseDto<Empty>())
        ));
    }

    [Fact]
    public async Task CreateTest()
    {
        var orderId = "00000000-0000-0000-0000-000000000001";
        var caHash = HashHelper.ComputeFrom(orderId).ToHex();
        
        var input = new CreateNftOrderRequestDto
        {
            NftSymbol = "LUCK",
            NftPicture = "http://127.0.0.1:9200/img/home/logo.png",
            MerchantName = MerchantName,
            MerchantOrderId = orderId,
            WebhookUrl = "http://127.0.0.1:9200/myWebhook",
            PriceSymbol = "ELF",
            PriceAmount = "100000000",
            CaHash = caHash
        };
        input.Signature = MerchantSignatureHelper.GetSignature(MerchantAccount.PrivateKey.ToHex(), input);

        var res = await _thirdPartOrderAppService.CreateNftOrderAsync(input);
        res.ShouldNotBeNull();
        res.Success.ShouldBe(true);
    }


    [Fact]
    public async Task AlchemyOrderUpdateTest()
    {
        await CreateTest();
        
        var orderId = "994864610797428736";
        var merchantOrderId = "03da9b8e-ee3b-de07-a53d-2e3cea36b2c4";

        var alchemyOrderRequestDto = new AlchemyNftOrderRequestDto
        {
            OrderNo = orderId,
            MerchantOrderNo = merchantOrderId,
            Amount = "100",
            Fiat = "USD",
            PayTime = "2022-07-08 15:18:43",
            PayType  = "CREDIT_CARD",
            Type = "MARKET",
            Name = "LUCK",
            Quantity = "1",
            UniqueId = "LUCK",
            AppId = "test",
            Message = "",
            Status = "PAY_SUCCESS",
            Signature = "EGugkNn2gz5qZ6etlfXGr2zBqrc="
        };
        var result = await _thirdPartNftOrderProcessorFactory
            .GetProcessor(ThirdPartNameType.Alchemy.ToString())
            .UpdateThirdPartNftOrderAsync(alchemyOrderRequestDto);
        result.ShouldNotBeNull();
        result.Success.ShouldBe(true);
    }

    [Fact]
    public async Task NftResultNotify()
    {
        await AlchemyOrderUpdateTest();

        var nftResult = new NftReleaseResultRequestDto
        {
            MerchantName = MerchantName,
            ReleaseResult = NftReleaseResult.SUCCESS.ToString(),
            ReleaseTransactionId = HashHelper.ComputeFrom("").ToHex(),
            MerchantOrderId = "00000000-0000-0000-0000-000000000001"
        };
        nftResult.Signature = MerchantSignatureHelper.GetSignature(MerchantAccount.PrivateKey.ToHex(), nftResult);

        var res = await _thirdPartOrderAppService.NoticeNftReleaseResultAsync(nftResult);
        res.ShouldNotBeNull();
        res.Success.ShouldBe(true);

    }
    
    
    
}


