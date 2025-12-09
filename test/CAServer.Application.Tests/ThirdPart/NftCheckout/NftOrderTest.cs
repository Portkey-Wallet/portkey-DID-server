using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using CAServer.BackGround.Provider;
using CAServer.Commons;
using CAServer.ThirdPart.Alchemy;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Order;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Provider;
using CAServer.Tokens.Provider;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace CAServer.ThirdPart.NftCheckout;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class NftOrderTest : ThirdPartTestBase
{
    private static readonly string MerchantName = "symbolMarket";

    private static readonly ECKeyPair MerchantAccount = CryptoHelper.FromPrivateKey(ByteArrayHelper
        .HexStringToByteArray("5945c176c4269dc2aa7daf7078bc63b952832e880da66e5f2237cdf79bc59c5f"));

    private static readonly ECKeyPair AnotherMerchant = CryptoHelper.FromPrivateKey(ByteArrayHelper
        .HexStringToByteArray("8515ad697d797625cc74d225c8efeab45bb8c267cd3271d3e1d9d44afabe6da7"));

    private readonly IThirdPartOrderAppService _thirdPartOrderAppService;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly INftCheckoutService _nftCheckoutService;
    private readonly NftOrderMerchantCallbackWorker _nftOrderMerchantCallbackWorker;
    private readonly NftOrderSettlementTransferWorker _nftOrderSettlementTransferWorker;
    private readonly NftOrderThirdPartOrderStatusWorker _nftOrderThirdPartOrderStatusWorker;
    private readonly NftOrderThirdPartNftResultNotifyWorker _orderThirdPartNftResultNotifyWorker;
    private readonly AlchemyProvider _alchemyProvider;
    private readonly IOrderStatusProvider _orderStatusProvider;

    private static readonly JsonSerializerSettings JsonSettings = JsonSettingsBuilder.New()
        .WithCamelCasePropertyNamesResolver()
        .IgnoreNullValue()
        .Build();

    public NftOrderTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _nftOrderSettlementTransferWorker = GetRequiredService<NftOrderSettlementTransferWorker>();
        _nftOrderThirdPartOrderStatusWorker = GetRequiredService<NftOrderThirdPartOrderStatusWorker>();
        _orderThirdPartNftResultNotifyWorker = GetRequiredService<NftOrderThirdPartNftResultNotifyWorker>();
        _nftCheckoutService = GetRequiredService<INftCheckoutService>();
        _thirdPartOrderAppService = GetRequiredService<IThirdPartOrderAppService>();
        _nftOrderMerchantCallbackWorker = GetRequiredService<NftOrderMerchantCallbackWorker>();
        _orderStatusProvider = GetRequiredService<IOrderStatusProvider>();
        _alchemyProvider = GetRequiredService<AlchemyProvider>();
        _testOutputHelper.WriteLine("publicKey = " + MerchantAccount.PublicKey.ToHex());
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockActivityProviderCaHolder("2e701e62-0953-4dd3-910b-dc6cc93ccb0d"));
        services.AddSingleton(MockRampOptions());
        services.AddSingleton(MockThirdPartOptions());
        services.AddSingleton(MockSecretProvider());
        services.AddSingleton(MockContractProvider());
        services.AddSingleton(MockGraphQlProvider());
        services.AddSingleton(MockTokenPrivider());

        MockHttpByPath(HttpMethod.Post, "/myWebhook", new CommonResponseDto<Empty>());
        MockHttpByPath(HttpMethod.Post, AlchemyApi.NftResultNotice.Path, new CommonResponseDto<Empty>());
        MockHttpByPath(HttpMethod.Get, AlchemyApi.QueryNftTrade.Path, new AlchemyBaseResponseDto<AlchemyNftOrderDto>(
            new AlchemyNftOrderDto
            {
                MerchantOrderNo = "03da9b8e-ee3b-de07-a53d-2e3cea36b2c4",
                Status = "PAY_SUCCESS"
            }));
        MockHttpByPath(HttpMethod.Post, "/myWebhookFail",
            new CommonResponseDto<Empty>().Error(new Exception("MockError")));

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
            UserAddress = "e8m4RyDLmt3ioCH7LzPvGPRags1Zv2255d3NpkD2fzA9SqmEQ",
            PaymentSymbol = "ELF",
            PaymentAmount = "100000000",
            MerchantAddress = Address.FromPublicKey(MerchantAccount.PublicKey).ToBase58(),
            CaHash = caHash
        };
        input.Signature = MerchantSignatureHelper.GetSignature(MerchantAccount.PrivateKey.ToHex(), input);

        var res = await _thirdPartOrderAppService.CreateNftOrderAsync(input);
        res.ShouldNotBeNull();
        res.Success.ShouldBe(true);


        var result = await _thirdPartOrderAppService.GetThirdPartOrdersAsync(new GetUserOrdersDto()
        {
            SkipCount = 0,
            MaxResultCount = 10
        });
        result.Items.Count.ShouldBe(1);
        result.Items[0].NftOrderSection.ShouldNotBeNull();
        _testOutputHelper.WriteLine(JsonConvert.SerializeObject(result, JsonSettings));
    }

    [Fact]
    public async Task CreateTest_InvalidParam()
    {
        var orderId = "00000000-0000-0000-0000-000000000001";
        var caHash = HashHelper.ComputeFrom(orderId).ToHex();

        var input = new CreateNftOrderRequestDto
        {
            NftSymbol = "LUCK",
            NftPicture = "http://127.0.0.1:9200/img/home/logo.png",
            MerchantName = MerchantName,
            MerchantOrderId = orderId,
            MerchantAddress = Address.FromPublicKey(MerchantAccount.PublicKey).ToBase58(),
            UserAddress = "e8m4RyDLmt3ioCH7LzPvGPRags1Zv2255d3NpkD2fzA9SqmEQ",
            WebhookUrl = "http://127.0.0.1:9200/myWebhook",
            PaymentSymbol = "ELF",
            PaymentAmount = "100000000",
            CaHash = caHash
        };
        input.Signature = MerchantSignatureHelper.GetSignature(MerchantAccount.PrivateKey.ToHex(), input);

        var errorPk = AnotherMerchant.PrivateKey.ToHex();
        input.Signature = MerchantSignatureHelper.GetSignature(errorPk, input);
        var res2 = await _thirdPartOrderAppService.CreateNftOrderAsync(input);
        res2.ShouldNotBeNull();
        res2.Message.ShouldContain("Invalid merchant signature");

        input.Signature = "ERROR SIGN";
        var res = await _thirdPartOrderAppService.CreateNftOrderAsync(input);
        res.ShouldNotBeNull();
        res.Message.ShouldContain("Verify merchant signature failed");
    }

    [Fact]
    public async Task QueryMerchantOrderNo()
    {
        await CreateTest();
        var input = new OrderQueryRequestDto
        {
            MerchantName = MerchantName,
            MerchantOrderId = "00000000-0000-0000-0000-000000000001"
        };
        input.Signature = MerchantSignatureHelper.GetSignature(MerchantAccount.PrivateKey.ToHex(), input);
        var list = await _thirdPartOrderAppService.QueryMerchantNftOrderAsync(input);
        list.Success.ShouldBe(true);
        list.Data.ShouldNotBeNull();
    }


    [Fact]
    public async Task AlchemyOrderUpdateTest()
    {
        await CreateTest();

        #region Mock Alchemy callback PAY_SUCCESS

        var orderId = "994864610797428736";
        var merchantOrderId = "03da9b8e-ee3b-de07-a53d-2e3cea36b2c4";

        var alchemyOrderRequestDto = new AlchemyNftOrderRequestDto
        {
            ["orderNo"] = orderId,
            ["merchantOrderNo"] = merchantOrderId,
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
            ["status"] = "PAY_SUCCESS",
            ["signature"] = "EGugkNn2gz5qZ6etlfXGr2zBqrc="
        };
        var result = await _nftCheckoutService
            .GetProcessor(ThirdPartNameType.Alchemy.ToString())
            .UpdateThirdPartNftOrderAsync(alchemyOrderRequestDto);
        result.ShouldNotBeNull();
        result.Success.ShouldBe(true);

        var order = await _orderStatusProvider.GetRampOrderAsync(Guid.Parse(merchantOrderId));
        order.ShouldNotBeNull();
        order.Status.ShouldBe(OrderStatusType.Transferring.ToString());

        #endregion

        #region update to Mined transactionId (just for test)

        order.TransactionId = MinedTxId;
        var updMindTxId = await _orderStatusProvider.UpdateRampOrderAsync(order);

        #endregion

        #region run worker to fix transaction status

        await _nftOrderSettlementTransferWorker.HandleAsync();
        order = await _orderStatusProvider.GetRampOrderAsync(Guid.Parse(merchantOrderId));
        order.ShouldNotBeNull();
        order.Status.ShouldBe(OrderStatusType.Finish.ToString());

        var nftOrder = await _orderStatusProvider.GetNftOrderAsync(Guid.Parse(merchantOrderId));
        nftOrder.ShouldNotBeNull();
        nftOrder.WebhookStatus.ShouldBe("SUCCESS");
        nftOrder.ThirdPartNotifyStatus.ShouldBe("SUCCESS");

        #endregion
    }

    [Fact]
    public async Task ExportTest()
    {
        await AlchemyOrderUpdateTest();

        var orderList = await _thirdPartOrderAppService.ExportOrderListAsync(new GetThirdPartOrderConditionDto(0, 100)
        {
            LastModifyTimeGt = "2023-11-01",
            LastModifyTimeLt = DateTime.UtcNow.AddDays(1).ToUtc8String(TimeHelper.DatePattern),
            TransDirectIn = new List<string> { TransferDirectionType.NFTBuy.ToString() },
            StatusIn = new List<string> { OrderStatusType.Finish.ToString() }
        }, OrderSectionEnum.NftSection, OrderSectionEnum.SettlementSection, OrderSectionEnum.OrderStateSection);

        orderList.ShouldNotBeNull();
        orderList.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AlchemyOrderUpdateTest_InvalidStatus()
    {
        await CreateTest();

        #region alchemy callback PAY_SUCCESS

        {
            var orderId = "994864610797428736";
            var merchantOrderId = "03da9b8e-ee3b-de07-a53d-2e3cea36b2c4";

            var alchemyOrderRequestDto = new AlchemyNftOrderRequestDto
            {
                ["orderNo"] = orderId,
                ["merchantOrderNo"] = merchantOrderId,
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
                ["status"] = "PAY_SUCCESS",
                ["signature"] = "EGugkNn2gz5qZ6etlfXGr2zBqrc="
            };
            var result = await _nftCheckoutService
                .GetProcessor(ThirdPartNameType.Alchemy.ToString())
                .UpdateThirdPartNftOrderAsync(alchemyOrderRequestDto);
            result.ShouldNotBeNull();
            result.Success.ShouldBe(true);
        }

        #endregion

        #region Alchemy callback NEW order status back

        {
            var orderId = "994864610797428736";
            var merchantOrderId = "03da9b8e-ee3b-de07-a53d-2e3cea36b2c4";

            var alchemyOrderRequestDto = new AlchemyNftOrderRequestDto
            {
                ["orderNo"] = orderId,
                ["merchantOrderNo"] = merchantOrderId,
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
                ["status"] = "NEW",
                ["signature"] = "W26721QQmJauXuCqfHWiC7oFg44="
            };
            var result = await _nftCheckoutService
                .GetProcessor(ThirdPartNameType.Alchemy.ToString())
                .UpdateThirdPartNftOrderAsync(alchemyOrderRequestDto);
            result.Success.ShouldBe(false);
        }

        #endregion
    }

    [Fact]
    public async Task MerchantWebhookFail()
    {
        #region create with fail webhook url

        var orderId = "00000000-0000-0000-0000-000000000001";
        var caHash = HashHelper.ComputeFrom(orderId).ToHex();

        var input = new CreateNftOrderRequestDto
        {
            NftSymbol = "LUCK",
            NftPicture = "http://127.0.0.1:9200/img/home/logo.png",
            MerchantName = MerchantName,
            MerchantOrderId = orderId,
            MerchantAddress = Address.FromPublicKey(MerchantAccount.PublicKey).ToBase58(),
            UserAddress = "e8m4RyDLmt3ioCH7LzPvGPRags1Zv2255d3NpkD2fzA9SqmEQ",
            WebhookUrl = "http://127.0.0.1:9200/myWebhookFail",
            PaymentSymbol = "ELF",
            PaymentAmount = "100000000",
            CaHash = caHash
        };
        input.Signature = MerchantSignatureHelper.GetSignature(MerchantAccount.PrivateKey.ToHex(), input);

        var res = await _thirdPartOrderAppService.CreateNftOrderAsync(input);
        res.ShouldNotBeNull();
        res.Success.ShouldBe(true);

        #endregion

        #region callback to merchant webhook url

        var achOrderId = "994864610797428736";
        var merchantOrderId = "03da9b8e-ee3b-de07-a53d-2e3cea36b2c4";

        var alchemyOrderRequestDto = new AlchemyNftOrderRequestDto
        {
            ["orderNo"] = achOrderId,
            ["merchantOrderNo"] = merchantOrderId,
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
            ["status"] = "PAY_SUCCESS",
            ["signature"] = "EGugkNn2gz5qZ6etlfXGr2zBqrc="
        };
        var result = await _nftCheckoutService
            .GetProcessor(ThirdPartNameType.Alchemy.ToString())
            .UpdateThirdPartNftOrderAsync(alchemyOrderRequestDto);
        result.ShouldNotBeNull();
        result.Success.ShouldBe(true);

        #endregion

        await _nftOrderMerchantCallbackWorker.HandleAsync();
    }


    [Fact]
    public async Task AlchemyOrderRefreshTest()
    {
        await CreateTest();

        #region call back NEW status only

        var orderId = "994864610797428736";
        var merchantOrderId = "03da9b8e-ee3b-de07-a53d-2e3cea36b2c4";

        var alchemyOrderRequestDto = new AlchemyNftOrderRequestDto
        {
            ["orderNo"] = orderId,
            ["merchantOrderNo"] = merchantOrderId,
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
            ["status"] = "NEW",
            ["signature"] = "W26721QQmJauXuCqfHWiC7oFg44="
        };
        var result = await _nftCheckoutService
            .GetProcessor(ThirdPartNameType.Alchemy.ToString())
            .UpdateThirdPartNftOrderAsync(alchemyOrderRequestDto);
        result.ShouldNotBeNull();
        result.Success.ShouldBe(true);

        #endregion

        await _nftOrderThirdPartOrderStatusWorker.HandleAsync();
    }
}