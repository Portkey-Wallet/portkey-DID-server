using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.BackGround.Provider.Treasury;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart.Alchemy;
using CAServer.ThirdPart.Dtos.Order;
using CAServer.ThirdPart.Dtos.ThirdPart;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace CAServer.ThirdPart.Ramp;

public partial class ThirdPartOrderAppServiceTest
{
    public Dictionary<string, string> TreasuryHeader()
    {
        var thirdPartOptions = ServiceProvider.GetRequiredService<IOptionsMonitor<ThirdPartOptions>>();
        return new Dictionary<string, string>
        {
            ["appid"] = thirdPartOptions.CurrentValue.Alchemy.AppId,
            ["timestamp"] = "1706513242364",
            ["sign"] = "5d802f4fa0f304fac9fe81689a7262b4b53c532b" // with mock secret "rampTest"
        };
    }
    
    [Fact]
    public async Task TreasuryPriceQuery_Alchemy()
    {
        var treasuryProcessorFactory = ServiceProvider.GetRequiredService<ITreasuryProcessorFactory>();
        var input = new AlchemyTreasuryPriceRequestDto()
        {
            Crypto = "USDT",
            Headers = TreasuryHeader(),
        };
        var result = await treasuryProcessorFactory.Processor(ThirdPartNameType.Alchemy.ToString())
            .GetPriceAsync(input);
        result.ShouldNotBeNull();
        var alchemyPrice = result as AlchemyTreasuryPriceResultDto;
        alchemyPrice.ShouldNotBeNull();
        alchemyPrice.Price.ShouldBe("1");
        alchemyPrice.NetworkList.ShouldNotBeNull();
        alchemyPrice.NetworkList.ShouldNotBeEmpty();
    }
    
    [Fact]
    public async Task TreasuryOrder_Alchemy()
    {
        MockRampLists();
        MockHttpByPath(AlchemyApi.CallBackTreasury, new AlchemyBaseResponseDto<Empty>());
    
        var treasuryProcessorFactory = ServiceProvider.GetRequiredService<ITreasuryProcessorFactory>();
        var treasuryOrderProvider = ServiceProvider.GetRequiredService<ITreasuryOrderProvider>();
        var orderAppService = ServiceProvider.GetRequiredService<IThirdPartOrderAppService>();
        var pendingTreasuryOrderWorker = ServiceProvider.GetRequiredService<PendingTreasuryOrderWorker>();
        var treasuryTxConfirmWorker = ServiceProvider.GetRequiredService<TreasuryTxConfirmWorker>();
    
        var rampOrder = await CreateThirdPartOrderAsyncTest();
    
        #region Notify treasury order
    
        var treasuryOrderRequest = new AlchemyTreasuryOrderRequestDto()
        {
            OrderNo = "1706370588684",
            Crypto = "USDT",
            Network = "ELF",
            Address = "ASh2Wt7nSEmYqnGxPPzp4pnVDU4uhj1XW9Se5VeZcX2UDdyjx",
            CryptoAmount = "40.00",
            CryptoPrice = "1",
            UsdtAmount = "34.56",
            Headers = TreasuryHeader()
        };
    
        await treasuryProcessorFactory.Processor(ThirdPartNameType.Alchemy.ToString())
            .NotifyOrderAsync(treasuryOrderRequest);
    
        #endregion
    
        #region Pending treasury order should be Pending status
        {
            var pendingData = await treasuryOrderProvider.QueryPendingTreasuryOrderAsync(
                new PendingTreasuryOrderCondition(0, 1)
                {
                    LastModifyTimeLt = DateTime.UtcNow.AddHours(1).ToUtcMilliSeconds(),
                    ExpireTimeGtEq = DateTime.UtcNow.ToUtcMilliSeconds()
                });
            pendingData.TotalCount.ShouldBe(1);
            pendingData.Items[0].Status.ShouldBe(OrderStatusType.Pending.ToString());
        }
        #endregion
    
        #region Webhook ramp order
    
        var webhookReq = new AlchemyOrderUpdateDto
        {
            OrderNo = "1706370588684",
            MerchantOrderNo = rampOrder.Id,
            Status = "NEW",
            Fiat = "USD",
            FiatAmount = "40.00",
            Address = "ASh2Wt7nSEmYqnGxPPzp4pnVDU4uhj1XW9Se5VeZcX2UDdyjx",
            Crypto = "USDT-aelf",
            Network = "ELF",
            NetworkFee = "0.01",
            RampFee = "5.43",
            CryptoQuantity = "40.000",
            Signature = "cd29b4371572abe3005fdfd43c93fafacebe946a"
        };
    
        var updateRamp = await orderAppService.OrderUpdateAsync(ThirdPartNameType.Alchemy.ToString(), webhookReq);
        updateRamp.Success.ShouldBeTrue();
    
        #endregion
    
        await pendingTreasuryOrderWorker.HandleAsync();
    
        #region Pending treasury order should be Pending status
        {
            var pendingData = await treasuryOrderProvider.QueryPendingTreasuryOrderAsync(
                new PendingTreasuryOrderCondition(0, 1)
                {
                    LastModifyTimeLt = DateTime.UtcNow.AddHours(1).ToUtcMilliSeconds(),
                    ExpireTimeGtEq = DateTime.UtcNow.ToUtcMilliSeconds()
                });
            pendingData.TotalCount.ShouldBe(1);
            pendingData.Items[0].Status.ShouldBe(OrderStatusType.Finish.ToString());
        }
        #endregion
    
        #region Order status is Transferring
        TreasuryOrderDto treasuryOrder;
        {
            var transferringOrder = await treasuryOrderProvider.QueryOrderAsync(new TreasuryOrderCondition(0, 10));
            transferringOrder.TotalCount.ShouldBe(1);
            transferringOrder.Items[0].Status.ShouldBe(OrderStatusType.Transferring.ToString());
            treasuryOrder = transferringOrder.Items[0];
        }
        #endregion
    
        // mock transactionId as MinedTxId
        treasuryOrder.TransactionId = MinedTxId;
        await orderAppService.UpdateTreasuryOrderAsync(treasuryOrder);
    
        // handle tx multi confirm
        await treasuryTxConfirmWorker.HandleAsync();
    
        #region Order status is Finish
        {
            var transferringOrder = await treasuryOrderProvider.QueryOrderAsync(new TreasuryOrderCondition(0, 10));
            transferringOrder.TotalCount.ShouldBe(1);
            transferringOrder.Items[0].Status.ShouldBe(OrderStatusType.Finish.ToString());
        }
        #endregion
    
    }
}