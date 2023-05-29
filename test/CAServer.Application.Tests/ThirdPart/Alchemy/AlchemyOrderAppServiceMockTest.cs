using CAServer.Options;
using Microsoft.Extensions.Options;

namespace CAServer.ThirdPart.Alchemy;

public partial class AlchemyOrderAppServiceTest
{
    private IOptions<ThirdPartOptions> getMockThirdPartOptions()
    {
        var thirdPartOptions = new ThirdPartOptions()
        {
            alchemy = new AlchemyOptions()
            {
                AppId = "12344fdsfdsfdsfsdfdsfsdfsdfdsfsdfa",
                AppSecret = "abadddfafdfdsfdsffdsfdsfdsfdsfds",
                BaseUrl = "http://localhost:9200/book/_search"
            }
        };
        return new OptionsWrapper<ThirdPartOptions>(thirdPartOptions);
    }
}