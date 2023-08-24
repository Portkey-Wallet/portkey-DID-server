using System;
using System.Net.Http;
using CAServer.Options;
using Microsoft.Extensions.Options;
using Moq;
using Xunit.Abstractions;

namespace CAServer.ThirdPart;

public static class ThirdPartMock
{
    
    public static IOptions<ThirdPartOptions> GetMockThirdPartOptions()
    {
        var mockOptions = new Mock<IOptions<ThirdPartOptions>>();
        mockOptions.Setup(ap => ap.Value).Returns(new ThirdPartOptions
        {
            alchemy = new AlchemyOptions()
            {
                AppId = "12344fdsfdsfdsfsdfdsfsdfsdfdsfsdfa",
                AppSecret = "abadddfafdfdsfdsffdsfdsfdsfdsfds",
                BaseUrl = "http://localhost:9200/book/_search",
                UpdateSellOrderUri = "/webhooks/off/merchant",
                FiatListUri = "/merchant/fiat/list",
                CryptoListUri = "/merchant/crypto/list",
                OrderQuoteUri = "/merchant/order/quote",
                GetTokenUri = "/merchant/getToken",
            },
            transak = new TransakOptions()
            {
                BaseUrl = "http://localhost:9200/transak/_search",
                AppId = "prod:test_appId",
                AppSecret = "prod:test_appSecret"
            },
            timer = new ThirdPartTimerOptions()
            {
                TimeoutMillis = 5000,
                DelaySeconds = 1,
            }
        });
        return mockOptions.Object;
    }
        
    public static IHttpClientFactory MockHttpFactory(ITestOutputHelper testOutputHelper,
        params Action<Mock<HttpMessageHandler>, ITestOutputHelper>[] mockActions)
    {
        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        foreach (var mockFunc in mockActions)
            mockFunc.Invoke(mockHandler, testOutputHelper);

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(_ => _.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://test.com/") });

        return httpClientFactoryMock.Object;
    }

}