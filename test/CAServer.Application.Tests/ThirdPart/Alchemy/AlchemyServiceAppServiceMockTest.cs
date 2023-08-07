using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Volo.Abp.Caching;
using Xunit.Abstractions;

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
    
    
    public static readonly Action<Mock<HttpMessageHandler>, ITestOutputHelper> MockAlchemyFiatListResponse =
        (mockHandler, testOutputHelper) =>
        {
            var expectedUri = AlchemyApi.GetFiatList;
            DateTimeOffset offset = DateTime.UtcNow.AddDays(7);
            var responseData = new AlchemyFiatListResponseDto()
            {
                Data = new List<AlchemyFiatDto>()
                {
                    new AlchemyFiatDto()
                    {
                        Currency = "USD"
                    }
                }
            };
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(responseData), Encoding.UTF8,
                    "application/json")
            };

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == expectedUri.Method &&
                        req.RequestUri.ToString().Contains(expectedUri.Path)),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(() =>
                {
                    testOutputHelper?.WriteLine($"Mock Http {expectedUri.Method.Method} to {expectedUri.Path}, resp={response}");
                    return Task.FromResult(response);
                });            
        };
    
    public static readonly Action<Mock<HttpMessageHandler>, ITestOutputHelper> MockAlchemyOrderQuoteList =
        (mockHandler, testOutputHelper) =>
        {
            var expectedUri = AlchemyApi.QueryPrice;
            DateTimeOffset offset = DateTime.UtcNow.AddDays(7);
            var responseData = new AlchemyOrderQuoteResponseDto()
            {
                Data = new AlchemyOrderQuoteDataDto()
                {
                    Crypto = "ELF",
                    CryptoPrice = "0.27",
                    CryptoQuantity = "0.27"
                }
            };
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(responseData), Encoding.UTF8,
                    "application/json")
            };

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == expectedUri.Method &&
                        req.RequestUri.ToString().Contains(expectedUri.Path)),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(() =>
                {
                    testOutputHelper?.WriteLine($"Mock Http {expectedUri.Method.Method} to {expectedUri.Path}, resp={response}");
                    return Task.FromResult(response);
                });            
        };
    
    public static readonly Action<Mock<HttpMessageHandler>, ITestOutputHelper> MockGetCryptoList =
        (mockHandler, testOutputHelper) =>
        {
            var expectedUri = AlchemyApi.GetCryptoList;
            DateTimeOffset offset = DateTime.UtcNow.AddDays(7);
            var responseData = new AlchemyCryptoListResponseDto()
            {
                Data = new List<AlchemyCryptoDto>()
                {
                    new AlchemyCryptoDto()
                    {
                        Crypto = "ELF",
                    }
                }
            };
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(responseData), Encoding.UTF8,
                    "application/json")
            };

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == expectedUri.Method &&
                        req.RequestUri.ToString().Contains(expectedUri.Path)),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(() =>
                {
                    testOutputHelper?.WriteLine($"Mock Http {expectedUri.Method.Method} to {expectedUri.Path}, resp={response}");
                    return Task.FromResult(response);
                });            
        };
}