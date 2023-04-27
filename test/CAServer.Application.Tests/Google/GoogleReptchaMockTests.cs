using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Options;
using CAServer.Tokens;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Volo.Abp.Caching;

namespace CAServer.Google;

public partial class GoogleRecapthaTests
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
        return new OptionsWrapper<GoogleRecaptchaOptions>(
            new GoogleRecaptchaOptions
            {
                Secret = "*****",
                VerifyUrl = "https://www.google.com/recaptcha/api/siteverify"
            });
    }

    private IDistributedCache<SendVerifierCodeInterfaceRequestCountCacheItem> GetMockDistributedCache()
    {
        var mockDistributedCache = new Mock<IDistributedCache<SendVerifierCodeInterfaceRequestCountCacheItem>>();
        mockDistributedCache.Setup(o => o.GetAsync(It.IsAny<string>(), default, default, default))
            .ReturnsAsync(new SendVerifierCodeInterfaceRequestCountCacheItem
            {
                SendVerifierCodeInterfaceRequestCount = 100
            });
        return mockDistributedCache.Object;
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