using System.Threading.Tasks;
using AElf;
using CAServer.Options;
using CAServer.Signature.Provider;
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


    private static ISecretProvider MockSecretProvider()
    {
        var secret = "abadddfafdfdsfdsffdsfdsfdsfdsfds";
        var option = GetMockThirdPartOptions();
        var mock = new Mock<ISecretProvider>();
        mock.Setup(ser => ser.GetAlchemyShaSignAsync(option.CurrentValue.Alchemy.AppId, It.IsAny<string>()))
            .Returns<string, string>((appid, source) => Task.FromResult(AlchemyHelper.GenerateAlchemyApiSign(appid + secret + source)));
        mock.Setup(ser => ser.GetAlchemyAesSignAsync(option.CurrentValue.Alchemy.AppId, It.IsAny<string>()))
            .Returns<string, string>((appid, source) => Task.FromResult(AlchemyHelper.AesEncrypt(source, secret)));
        mock.Setup(ser => ser.GetAlchemyHmacSignAsync(option.CurrentValue.Alchemy.AppId, It.IsAny<string>()))
            .Returns<string, string>((appid, source) => Task.FromResult(AlchemyHelper.HmacSign(source, secret)));
        return mock.Object;
    }
    
}