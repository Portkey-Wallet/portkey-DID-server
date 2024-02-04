using CAServer.Options;
using CAServer.ThirdPart.Dtos.ThirdPart;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp.Caching;

namespace CAServer.ThirdPart.Alchemy;

public partial class AlchemyServiceAppServiceTest
{
    private IOptionsMonitor<ThirdPartOptions> getMockThirdPartOptions()
    {
        var thirdPartOptions = new ThirdPartOptions()
        {
            Alchemy = new AlchemyOptions()
            {
                AppId = "12344fdsfdsfdsfsdfdsfsdfsdfdsfsdfa",
                BaseUrl = "http://localhost:9200/book/_search",
                SkipCheckSign = true
            },
            Timer = new ThirdPartTimerOptions()
            {
                TimeoutMillis = 5000,
                DelaySeconds = 1,
            }
        };
        var mockOption = new Mock<IOptionsMonitor<ThirdPartOptions>>();
        mockOption.Setup(o => o.CurrentValue).Returns(thirdPartOptions);
        return mockOption.Object;
    }

    private IDistributedCache<AlchemyOrderQuoteDataDto> GetMockAlchemyOrderQuoteDto()
    {
        var mockCache = new Mock<IDistributedCache<AlchemyOrderQuoteDataDto>>();

        mockCache.Setup(t => t.GetAsync(It.IsAny<string>(), default, default, default))
            .ReturnsAsync(new AlchemyOrderQuoteDataDto()
            {
                Crypto = "ELF",
                CryptoPrice = "0.27",
                CryptoQuantity = "0.27"
            });
        return mockCache.Object;
    }
}