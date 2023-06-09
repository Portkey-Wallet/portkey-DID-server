using System.Collections.Generic;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp.Caching;

namespace CAServer.ThirdPart.Alchemy;

public partial class AlchemyServiceAppServiceTest
{
    private IOptions<ThirdPartOptions> getMockThirdPartOptions()
    {
        var thirdPartOptions = new ThirdPartOptions()
        {
            alchemy = new AlchemyOptions()
            {
                AppId = "12344fdsfdsfdsfsdfdsfsdfsdfdsfsdfa",
                AppSecret = "abadddfafdfdsfdsffdsfdsfdsfdsfds",
                BaseUrl = "http://localhost:9200/book/_search",
                SkipCheckSign = true
            },
            timer = new ThirdPartTimerOptions()
            {
                TimeoutMillis = 5000,
                DelaySeconds = 1,
            }
        };
        return new OptionsWrapper<ThirdPartOptions>(thirdPartOptions);
    }

    private IDistributedCache<List<AlchemyFiatDto>> GetMockAlchemyFiatDto()
    {
        var mockCache = new Mock<IDistributedCache<List<AlchemyFiatDto>>>();

        mockCache.Setup(t => t.GetAsync(It.IsAny<string>(), default, default, default))
            .ReturnsAsync(new List<AlchemyFiatDto>()
            {
                new AlchemyFiatDto()
                {
                    Currency = "USD"
                }
            });
        return mockCache.Object;
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