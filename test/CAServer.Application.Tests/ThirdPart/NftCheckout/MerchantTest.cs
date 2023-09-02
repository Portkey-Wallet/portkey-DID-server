using System;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using CAServer.ThirdPart.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace CAServer.ThirdPart.NftCheckout;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class MerchantTest : CAServerApplicationTestBase
{
    private static readonly ECKeyPair MerchantAccount = CryptoHelper.FromPrivateKey(ByteArrayHelper
        .HexStringToByteArray("5945c176c4269dc2aa7daf7078bc63b952832e880da66e5f2237cdf79bc59c5f"));

    private readonly IThirdPartOrderAppService _thirdPartOrderAppService;
    private readonly ITestOutputHelper _testOutputHelper;

    public MerchantTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _thirdPartOrderAppService = GetRequiredService<IThirdPartOrderAppService>();
        _testOutputHelper.WriteLine("publicKey = " + MerchantAccount.PublicKey.ToHex());
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockRandomActivityProviderCaHolder());
        services.AddSingleton(MockThirdPartOptions());
    }

    [Fact]
    public async Task CreateTest()
    {
        
        var input = new CreateNftOrderRequestDto
        {
            NftSymbol = "LUCK",
            NftPicture = "http://127.0.0.1:8080/img/home/logo.png",
            MerchantName = "symbolMarket",
            MerchantOrderId = new Guid().ToString(),
            WebhookUrl = "http://127.0.0.1:8080/myWebhook",
            PriceSymbol = "ELF",
            PriceAmount = "100000000",
            CaHash = HashHelper.ComputeFrom("").ToHex(),
        };
        input.Signature = MerchantSignatureHelper.GetSignature(MerchantAccount.PrivateKey.ToHex(), input);

        var res = await _thirdPartOrderAppService.CreateNftOrderAsync(input);
        res.ShouldNotBeNull();
        res.Success.ShouldBe(true);
    }
}