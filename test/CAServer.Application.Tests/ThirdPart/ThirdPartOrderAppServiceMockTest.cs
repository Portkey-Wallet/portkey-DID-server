using System.Collections.Generic;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Moq;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.ThirdPart;

public partial class ThirdPartOrderAppServiceTest
{
    private IThirdPartOrderProvider MockThirdPartOrderProvider()
    {
        var mockThirdPartOrderProvider = new Mock<IThirdPartOrderProvider>();
        mockThirdPartOrderProvider.Setup(o => o.GetThirdPartOrdersByPageAsync(It.IsAny<GetThirdPartOrderConditionDto>()))
            .ReturnsAsync(
                new PagedResultDto<OrderDto>()
                {
                    TotalCount = 1,
                    Items = new List<OrderDto>()
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