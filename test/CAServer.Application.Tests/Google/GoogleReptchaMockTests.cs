using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Cache;
using CAServer.Hub;
using CAServer.Options;
using Microsoft.Extensions.Options;
using Moq;
using Scriban.Runtime.Accessors;

namespace CAServer.Google;

public partial class GoogleRecaptchaTests
{
    private IHttpClientFactory GetMockHttpClientFactory()
    {
        var clientHandlerStub = new DelegatingHandlerStub();
        clientHandlerStub.InnerHandler = new HttpClientHandler();
        var client = new HttpClient(clientHandlerStub);

        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        var factory = mockFactory.Object;
        return factory;
    }

    private IOptions<GoogleRecaptchaOptions> GetGoogleRecaptchaOptions()
    {
        var dic = new Dictionary<string, string>();
        dic.Add("IOS","bcd");
        return new OptionsWrapper<GoogleRecaptchaOptions>(
            new GoogleRecaptchaOptions
            {
                SecretMap = dic,
                VerifyUrl = "https://www.google.com/recaptcha/api/siteverify"
            });
    }
    
    private ICacheProvider GetMockCacheProvider()
    {
        return new MockCacheProvider();
    }


}

public class DelegatingHandlerStub : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

    public DelegatingHandlerStub()
    {
        _handlerFunc = (request, cancellationToken) => Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK,Content = new StringContent("\"success\": true")});
    }


    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handlerFunc(request, cancellationToken);
    }
}