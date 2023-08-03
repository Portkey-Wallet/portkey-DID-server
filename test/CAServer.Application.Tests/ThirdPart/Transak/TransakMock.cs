using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Options;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;

namespace CAServer.ThirdPart.Transak;

public sealed partial class TransakTest
{

    private IOptions<ThirdPartOptions> MockThirdPartOptions()
    {
        var thirdPartOptions = new ThirdPartOptions()
        {
            transak = new TransakOptions()
            {
                BaseUrl = "http://transak.test.com",
                AppId = "test_appId",
                AppSecret = "test_appSecret"
            }
        };
        return new OptionsWrapper<ThirdPartOptions>(thirdPartOptions);
    }
    
    public static IHttpClientFactory MockHttpFactory(params Action<Mock<HttpMessageHandler>>[] mockActions)
    {
        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        foreach (var mockFunc in mockActions)
            mockFunc.Invoke(mockHandler);
        
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(_ => _.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://test.com/") });

        return httpClientFactoryMock.Object;
    }


    public static readonly Action<Mock<HttpMessageHandler>> MockRefreshAccessToken = mockHandler =>
    {
        var expectedUri = TransakApi.RefreshAccessToken;
        DateTimeOffset offset = DateTime.UtcNow.AddDays(7);
        var responseData = new Dictionary<string, object>
        {
            ["data"] = new Dictionary<string, object>
            {
                ["accessToken"] = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJBUElfS0VZIjoiMDljMDU2ZmQtZDQyMy00NmQ5LWE2NDEtZTRhN2ExZTdkZTMzIiwiaWF0IjoxNjkwOTU1OTQxLCJleHAiOjE2OTE1NjA3NDF9.j3mn6ctBKPnkkiYRchg-BzGgdI9ZfUgH3bbC0QIGtkM",
                ["expiresAt"] = offset.ToUnixTimeSeconds()
            }
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(responseData), Encoding.UTF8, "application/json")
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == expectedUri.Method &&
                    req.RequestUri.ToString().Contains(expectedUri.Path)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    };


}