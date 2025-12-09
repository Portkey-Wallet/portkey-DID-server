using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Options;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;

namespace CAServer.IpWhiteList;

public partial class IpWhiteListTests
{
    private IHttpClientFactory GetMockHttpClientFactory()
    {
        var clientHandlerStub = new DelegatingHandlerStub();
        var client = new HttpClient(clientHandlerStub);

        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        var factory = mockFactory.Object;
        return factory;
    }
    
    private IOptionsSnapshot<AddToWhiteListUrlsOptions> GetAddToWhiteListUrlsOptions()
    {
        var urls = new List<string>()
        {
            "api/mockUrl1",
            "api/mockUrl2",
            "/api/mockUrl3",
        };
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<AddToWhiteListUrlsOptions>>();
        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new AddToWhiteListUrlsOptions
            {
                Urls = urls,
                BaseAddUrl = "http://127.0.0.1/api/mockUrl1",
                BaseCheckUrl = "http://127.0.0.1/api/mockUrl2"
            });
        return mockOptionsSnapshot.Object;
    }
    
    
}

public class DelegatingHandlerStub : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

    public DelegatingHandlerStub()
    {
        _handlerFunc = (request, cancellationToken) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(
                new ResponseResultDto<CheckUserIpInWhiteListResponseDto>
                {
                    Success = true,
                    Data = new CheckUserIpInWhiteListResponseDto
                    {
                        IsInWhiteList = true
                    }
                }))
        });
    }

    public DelegatingHandlerStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return _handlerFunc(request, cancellationToken);
    }
}