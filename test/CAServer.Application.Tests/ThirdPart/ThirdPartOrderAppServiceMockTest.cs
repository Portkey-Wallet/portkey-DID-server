using System;
using System.Collections.Generic;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Moq;
using Nest;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace CAServer.ThirdPart;

public partial class ThirdPartOrderAppServiceTest
{
    private IThirdPartOrderProvider getMockTokenPriceGrain()
    {
        var mockThirdPartOrderProvider = new Mock<IThirdPartOrderProvider>();
        mockThirdPartOrderProvider.Setup(o => o.GetThirdPartOrdersByPageAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(
                new List<OrderDto>()
                {
                    new OrderDto()
                    {
                        Address = "Address",
                        MerchantName = "MerchantName",
                        Crypto = "Crypto",
                        CryptoPrice = "CryptoPrice",
                        Fiat = "Fiat",
                        FiatAmount = "FiatAmount",
                        LastModifyTime = "LastModifyTime"
                    }
                }
            );
                      

        return mockThirdPartOrderProvider.Object;

    }
    private IOrderGrain getMockOrderGrain()
    {
        var mockockOrderGrain = new Mock<IOrderGrain>();
        mockockOrderGrain.Setup(o => o.CreateUserOrderAsync(It.IsAny<OrderGrainDto>()))
            .ReturnsAsync((OrderGrainDto dtos) =>
                new GrainResultDto<OrderGrainDto>()
                {
                    Success = true,
                    Data = new OrderGrainDto()
                    {
                        Address = "Address",
                        MerchantName = "MerchantName",
                        Crypto = "Crypto",
                        CryptoPrice = "CryptoPrice",
                        Fiat = "Fiat",
                        FiatAmount = "FiatAmount",
                        LastModifyTime = "LastModifyTime"
                    }
                    
                }
            );                      

        return mockockOrderGrain.Object;

    }
    private IDistributedEventBus getMockDistributedEventBus()
    {
        var mockDistributedEventBus = new Mock<IDistributedEventBus>();
        return mockDistributedEventBus.Object;
    }
}