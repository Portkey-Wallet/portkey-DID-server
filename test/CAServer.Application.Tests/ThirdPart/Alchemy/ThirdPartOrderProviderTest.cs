using System;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CAServer.ThirdPart.Alchemy;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public sealed class ThirdPartOrderProviderTest : CAServerApplicationTestBase
{
    private readonly IThirdPartOrderProvider _orderProvider;
    private readonly IOrderProcessorFactory _orderProcessorFactory;

    public ThirdPartOrderProviderTest()
    {
        _orderProcessorFactory = GetRequiredService<IOrderProcessorFactory>();
        _orderProvider = GetRequiredService<IThirdPartOrderProvider>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(AlchemyOrderAppServiceTest.GetMockThirdPartOptions());
    }

    [Fact]
    public async Task GetThirdPartOrdersByPageAsyncTest()
    {
        var result = await _orderProvider.GetThirdPartOrdersByPageAsync(Guid.Empty, 0, 10);
        result.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetUnCompletedThirdPartOrderTest()
    {
        var orderCreatedDto = await _orderProcessorFactory.GetProcessor(MerchantNameType.Alchemy.ToString())
            .CreateThirdPartOrderAsync(new CreateUserOrderDto 
            {
                OrderId = "xunit",
                TransDirect = TransferDirectionType.TokenSell.ToString(),
                MerchantName = MerchantNameType.Alchemy.ToString(),
            });
        orderCreatedDto?.Id.ShouldNotBeNullOrEmpty();
        
        var unCompletedOrders = await _orderProvider.GetUnCompletedThirdPartOrdersAsync();
        unCompletedOrders.Count.ShouldBe(0);
        
        // update order to StartPayment status
        var input = new AlchemyOrderUpdateDto
        {
            MerchantOrderNo = orderCreatedDto?.Id,
            Status = "3", // StartPayment
            Address = "Address",
            Crypto = "Crypto",
            OrderNo = "OrderNo",
            Network = "AELF",
            Signature = "46a417ea93da116cdc0259996c124b7f4f10d503"
        };
        var result = await _orderProcessorFactory.GetProcessor(MerchantNameType.Alchemy.ToString()).OrderUpdate(input);
        result.Success.ShouldBe(true);
        unCompletedOrders = await _orderProvider.GetUnCompletedThirdPartOrdersAsync();
        unCompletedOrders.Count.ShouldBe(1);
    }
}