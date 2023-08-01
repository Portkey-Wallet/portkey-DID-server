using CAServer.Options;
using Microsoft.Extensions.Options;
using Moq;

namespace CAServer.ThirdPart.Alchemy;

public partial class AlchemyOrderAppServiceTest
{
    private IOptions<ThirdPartOptions> getMockThirdPartOptions()
    {
        var mockOptions = new Mock<IOptions<ThirdPartOptions>>();
        mockOptions.Setup(ap => ap.Value).Returns(new ThirdPartOptions
        {
            alchemy = new AlchemyOptions()
            {
                AppId = "12344fdsfdsfdsfsdfdsfsdfsdfdsfsdfa",
                AppSecret = "abadddfafdfdsfdsffdsfdsfdsfdsfds",
                BaseUrl = "http://localhost:9200/book/_search",
            },
            timer = new ThirdPartTimerOptions()
            {
                TimeoutMillis = 5000,
                DelaySeconds = 1,
            }
        });
        return mockOptions.Object;
    }
}