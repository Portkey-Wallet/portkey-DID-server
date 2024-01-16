using CAServer.Options;
using Microsoft.Extensions.Options;
using Moq;

namespace CAServer.ThirdPart.Alchemy;

public sealed partial class AlchemyOrderAppServiceTest
{
    private static IOptionsMonitor<ThirdPartOptions> GetMockThirdPartOptions()
    {
        var thirdPartOptions = new ThirdPartOptions()
        {
            Alchemy = new AlchemyOptions()
            {
                AppId = "12344fdsfdsfdsfsdfdsfsdfsdfdsfsdfa",
                BaseUrl = "http://localhost:9200/book/_search",
            },
            Transak = new TransakOptions
            {
                AppId = "transakAppId",
                BaseUrl = "http://127.0.0.1:9200"
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
}