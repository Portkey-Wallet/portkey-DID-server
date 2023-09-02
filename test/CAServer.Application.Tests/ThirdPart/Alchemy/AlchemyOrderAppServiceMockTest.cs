using CAServer.Options;
using Microsoft.Extensions.Options;

namespace CAServer.ThirdPart.Alchemy;

public sealed partial class AlchemyOrderAppServiceTest
{
    private static IOptions<ThirdPartOptions> GetMockThirdPartOptions()
    {
        var thirdPartOptions = new ThirdPartOptions()
        {
            Alchemy = new AlchemyOptions()
            {
                AppId = "12344fdsfdsfdsfsdfdsfsdfsdfdsfsdfa",
                AppSecret = "abadddfafdfdsfdsffdsfdsfdsfdsfds",
                BaseUrl = "http://localhost:9200/book/_search",
            },
            Timer = new ThirdPartTimerOptions()
            {
                TimeoutMillis = 5000,
                DelaySeconds = 1,
            }
        };
        return new OptionsWrapper<ThirdPartOptions>(thirdPartOptions);
    }
}