using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace CAServer.ThirdPart.Transak;

public sealed partial class TransakTest : CAServerApplicationTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ITransakServiceAppService _transakServiceAppService;
    private readonly IOrderProcessorFactory _orderProcessorFactory;

    public TransakTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _transakServiceAppService = GetRequiredService<ITransakServiceAppService>();
        _orderProcessorFactory = GetRequiredService<IOrderProcessorFactory>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockThirdPartOptions());
        services.AddSingleton(MockHttpFactory(
            MockRefreshAccessToken
        ));
    }

    [Fact]
    public async Task<TransakOrderUpdateEventDto> DecodeJwtTest()
    {
        var data =
            "eyJhbGciOiJIUzI1NiJ9.eyJ3ZWJob29rRGF0YSI6eyJpZCI6IjkxNTFmYWExLWU2OWItNGEzNi1iOTU5LTNjNGY4OTRhZmI2OCIsIndhbGxldEFkZHJlc3MiOiIweDg2MzQ5MDIwZTkzOTRiMkJFMWIxMjYyNTMxQjBDMzMzNWZjMzJGMjAiLCJjcmVhdGVkQXQiOiIyMDIwLTAyLTE3VDAxOjU1OjA1LjA5NVoiLCJzdGF0dXMiOiJBV0FJVElOR19QQVlNRU5UX0ZST01fVVNFUiIsImZpYXRDdXJyZW5jeSI6IklOUiIsInVzZXJJZCI6IjY1MzE3MTMxLWNkOTUtNDE5YS1hNTBjLTc0N2QxNDJmODNlOSIsImNyeXB0b0N1cnJlbmN5IjoiQ0RBSSIsImlzQnV5T3JTZWxsIjoiQlVZIiwiZmlhdEFtb3VudCI6MTExMCwiZnJvbVdhbGxldEFkZHJlc3MiOiIweDA4NWVlNjcxMzJlYzQyOTdiODVlZDVkMWI0YzY1NDI0ZDM2ZmRhN2QiLCJ3YWxsZXRMaW5rIjoiaHR0cHM6Ly9yaW5rZWJ5LmV0aGVyc2Nhbi5pby9hZGRyZXNzLzB4ODYzNDkwMjBlOTM5NGIyQkUxYjEyNjI1MzFCMEMzMzM1ZmMzMkYyMCN0b2tlbnR4bnMiLCJhbW91bnRQYWlkIjowLCJwYXJ0bmVyT3JkZXJJZCI6IjIxODM3MjE4OTMiLCJwYXJ0bmVyQ3VzdG9tZXJJZCI6IjIxODM3MjE4OTMiLCJyZWRpcmVjdFVSTCI6Imh0dHBzOi8vZ29vZ2xlLmNvbSIsImNvbnZlcnNpb25QcmljZSI6MC42NjM4NDcxNjQzNjg2MDYsImNyeXB0b0Ftb3VudCI6NzMxLjM0LCJ0b3RhbEZlZSI6NS41MjY1Mjc2NDMzNjg2NCwiYXV0b0V4cGlyZXNBdCI6IjIwMjAtMDItMTZUMTk6NTU6MDUtMDc6MDAiLCJyZWZlcmVuY2VDb2RlIjoyMjYwNTZ9LCJldmVudElEIjoiT1JERVJfQ1JFQVRFRCIsImNyZWF0ZWRBdCI6IjIwMjMtMDgtMDJUMDc6MDI6NTMuMjQ5WiJ9.61L0In4DXAuGhkIKthGObdph47kCSyhtoecOkTAIWcE";
        var token = await _transakServiceAppService.GetAccessTokenAsync();
        var json = TransakHelper.DecodeJwt(data, token.Item2);
        _testOutputHelper.WriteLine(json);

        var orderUpdateEventDto = JsonConvert.DeserializeObject<TransakOrderUpdateEventDto>(json);
        orderUpdateEventDto.WebhookOrder.ShouldNotBeNull();
        orderUpdateEventDto.WebhookOrder.Id.ShouldNotBeNull();

        return orderUpdateEventDto;
    }

    [Fact]
    public async Task UpdateOrder()
    {
        var order = await DecodeJwtTest();
        order.WebhookOrder.PartnerOrderId =
            ThirdPartHelper.GenerateOrderId(MerchantNameType.Transak.ToString(), order.WebhookOrder.Id).ToString();

        // create new order
        var createRes = await _orderProcessorFactory.GetProcessor(MerchantNameType.Transak.ToString())
            .CreateThirdPartOrderAsync(new CreateUserOrderDto
            {
                MerchantName = MerchantNameType.Transak.ToString(),
                TransDirect = TransferDirectionType.TokenBuy.ToString(),
                OrderId = order.WebhookOrder.Id
            });
        createRes.ShouldNotBeNull();
        createRes.Id.ShouldBe(order.WebhookOrder.PartnerOrderId);

        // test update order
        var token = await _transakServiceAppService.GetAccessTokenAsync();
        var transakEventRawDataDto = new TransakEventRawDataDto()
        {
            Data = TransakHelper.EncodeJwt(new Dictionary<string, string>
            {
                ["webhookData"] = JsonConvert.SerializeObject(order.WebhookOrder)
            }, token.Item2)
        };
        var res = await _orderProcessorFactory.GetProcessor(MerchantNameType.Transak.ToString())
            .OrderUpdate(transakEventRawDataDto);
        _testOutputHelper.WriteLine(JsonConvert.SerializeObject(res));
        res.Success.ShouldBeTrue();
    }
}